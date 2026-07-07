using Microsoft.Extensions.Logging;
using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Validation;

namespace SkyRoute.Application.Services;

/// <summary>
/// Orchestrates booking creation per architecture-plan.md Section 3.3 / feature-booking-flow.md
/// Section 5. Structural validation (step 1) runs in BookingController before this service is
/// called (BL-014 ValidateStructure); this class performs steps 2–7: authoritative route-type
/// re-resolution, document validation against that resolved route type, server-side total-price
/// recomputation, collision-checked reference generation, persistence, and response mapping.
/// </summary>
public sealed class BookingService : IBookingService
{
    private const int MaxReferenceGenerationAttempts = 10;

    private readonly IBookingStore _store;
    private readonly ITenantContext _tenantContext;
    private readonly BookingReferenceGenerator _referenceGenerator;
    private readonly RouteTypeResolver _routeTypeResolver;
    private readonly BookingRequestValidator _validator;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingStore store,
        ITenantContext tenantContext,
        BookingReferenceGenerator referenceGenerator,
        RouteTypeResolver routeTypeResolver,
        BookingRequestValidator validator,
        ILogger<BookingService> logger)
    {
        _store = store;
        _tenantContext = tenantContext;
        _referenceGenerator = referenceGenerator;
        _routeTypeResolver = routeTypeResolver;
        _validator = validator;
        _logger = logger;
    }

    public async Task<BookingResponse> CreateBookingAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        // Step 2 (BR-003, DP-016, NFR-DATA-004): authoritative server-side route-type
        // resolution, independent of anything the client submitted.
        var routeType = _routeTypeResolver.Resolve(request.Flight.Origin, request.Flight.Destination);

        // Step 3: document type/number validation against the resolved route type.
        var documentErrors = _validator.ValidateDocuments(request, routeType);
        if (documentErrors.Count > 0)
        {
            throw new BookingValidationException(documentErrors);
        }

        // Step 4 (BR-006, NFR-DATA-002): server-side total price recomputation — there is no
        // client-submitted total to trust or distrust in this contract (AD-004/AD-005).
        var totalPrice = Math.Round(
            request.Flight.PricePerPassenger!.Value * request.PassengerCount,
            2,
            MidpointRounding.AwayFromZero);

        var tenantId = _tenantContext.TenantId;

        // Step 5/6 (BR-004, NFR-DATA-001, Gap-fill BF-03, code review finding CR-003): generate
        // a candidate reference and attempt to persist it, bounded retry. CreateAsync's atomic
        // TryAdd (not the preceding ExistsAsync pre-check alone) is the actual source of truth
        // for uniqueness — a DuplicateBookingReferenceException triggers a retry with a freshly
        // generated candidate, closing the check-then-act TOCTOU race that existed when
        // uniqueness was enforced only by a separate ExistsAsync call ahead of CreateAsync.
        var created = await CreateBookingWithUniqueReferenceAsync(
            routeType, tenantId, request, totalPrice, cancellationToken);

        _logger.LogInformation("Booking record created for reference {BookingReference}", created.BookingReference);

        // Step 7 (FR-044): map to the response contract.
        return MapToResponse(created);
    }

    private async Task<Booking> CreateBookingWithUniqueReferenceAsync(
        RouteType routeType,
        string tenantId,
        BookingRequest request,
        decimal totalPrice,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxReferenceGenerationAttempts; attempt++)
        {
            var candidate = _referenceGenerator.GenerateBookingReference(routeType);

            // Fast-path optimization only: skips an obviously-taken candidate without paying
            // for a CreateAsync round-trip. Not relied upon for correctness — CreateAsync's
            // atomic TryAdd below is what actually enforces uniqueness.
            if (await _store.ExistsAsync(candidate, tenantId, cancellationToken))
            {
                continue;
            }

            var booking = new Booking
            {
                BookingReference = candidate,
                Flight = new BookingFlightSnapshot
                {
                    Provider = request.Flight.Provider!,
                    FlightNumber = request.Flight.FlightNumber!,
                    Origin = request.Flight.Origin!,
                    Destination = request.Flight.Destination!,
                    DepartureDateTime = request.Flight.DepartureDateTime!.Value,
                    ArrivalDateTime = request.Flight.ArrivalDateTime!.Value,
                    CabinClass = request.Flight.CabinClass!,
                    PricePerPassenger = request.Flight.PricePerPassenger!.Value,
                },
                Passengers = request.Passengers.Select(p => new PassengerDetail
                {
                    FullName = p.FullName!,
                    Email = p.Email!,
                    DocumentType = p.DocumentType!,
                    DocumentNumber = p.DocumentNumber!,
                }).ToList(),
                CabinClass = request.Flight.CabinClass!,
                TotalPrice = totalPrice,
                CreatedAtUtc = DateTime.UtcNow,
                TenantId = tenantId,
            };

            try
            {
                return await _store.CreateAsync(booking, tenantId, cancellationToken);
            }
            catch (DuplicateBookingReferenceException)
            {
                // Collision detected by the store's atomic add, despite the ExistsAsync
                // fast-path above having reported "unused" (a genuine TOCTOU race under
                // concurrent load) — retry with a newly generated reference.
            }
        }

        // Gap-fill BF-03: retry cap exhausted — propagates to ApiExceptionMiddleware as a
        // generic 500. Not an anticipated real path given the 36^6 keyspace.
        throw new InvalidOperationException(
            $"Unable to generate a unique booking reference after {MaxReferenceGenerationAttempts} attempts.");
    }

    private static BookingResponse MapToResponse(Booking booking) => new()
    {
        BookingReference = booking.BookingReference,
        Flight = new BookingFlightResponse
        {
            Provider = booking.Flight.Provider,
            FlightNumber = booking.Flight.FlightNumber,
            Origin = booking.Flight.Origin,
            Destination = booking.Flight.Destination,
            DepartureDateTime = booking.Flight.DepartureDateTime,
            ArrivalDateTime = booking.Flight.ArrivalDateTime,
            CabinClass = booking.Flight.CabinClass,
            PricePerPassenger = booking.Flight.PricePerPassenger,
        },
        TotalPrice = booking.TotalPrice,
        Passengers = booking.Passengers.Select(p => new PassengerNameResponse { FullName = p.FullName }).ToList(),
        CreatedAtUtc = booking.CreatedAtUtc,
    };
}
