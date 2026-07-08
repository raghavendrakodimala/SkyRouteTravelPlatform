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
/// called (BL-014 ValidateStructure); this class performs the remaining steps: authoritative
/// server-side re-resolution of the whole identifying flight snapshot — route AND fare — from
/// Provider/FlightNumber/CabinClass (SEC-001, AUD-025/028/033), validation of the client
/// snapshot against those resolved values, route-type derivation from the RESOLVED route,
/// document validation against that route type, server-side total-price recomputation from the
/// resolved fare, collision-checked reference generation, persistence of the canonical resolved
/// snapshot, and response mapping.
/// </summary>
public sealed class BookingService : IBookingService
{
    private const int MaxReferenceGenerationAttempts = 10;

    private readonly IBookingStore _store;
    private readonly ITenantContext _tenantContext;
    private readonly BookingReferenceGenerator _referenceGenerator;
    private readonly RouteTypeResolver _routeTypeResolver;
    private readonly FlightFareResolver _fareResolver;
    private readonly BookingRequestValidator _validator;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingStore store,
        ITenantContext tenantContext,
        BookingReferenceGenerator referenceGenerator,
        RouteTypeResolver routeTypeResolver,
        FlightFareResolver fareResolver,
        BookingRequestValidator validator,
        ILogger<BookingService> logger)
    {
        _store = store;
        _tenantContext = tenantContext;
        _referenceGenerator = referenceGenerator;
        _routeTypeResolver = routeTypeResolver;
        _fareResolver = fareResolver;
        _validator = validator;
        _logger = logger;
    }

    public async Task<BookingResponse> CreateBookingAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        // Step 2 (SEC-001, AUD-025/028/033, BR-006, NFR-DATA-002/004): authoritatively
        // re-resolve the WHOLE identifying flight snapshot server-side from the identifying
        // fields Provider/FlightNumber/CabinClass — the client-submitted Origin/Destination and
        // fare are NEVER trusted. A single provider-schedule lookup surfaces both the real route
        // (Origin/Destination) and the re-derived fare (BaseFare/PricePerPassenger) the same
        // provider pricing logic produced at search time.
        var flightResolved = _fareResolver.TryResolveFare(
            request.Flight.Provider,
            request.Flight.FlightNumber,
            request.Flight.CabinClass,
            out var resolvedBaseFare,
            out var resolvedPricePerPassenger,
            out var resolvedOrigin,
            out var resolvedDestination);

        // Step 2b: validate the client snapshot (route identity + fare) against the resolved
        // flight. A forged Origin/Destination (an international flight declared as a domestic
        // pair to bypass BR-003's passport gate — AUD-025/028/033) or a fabricated fare
        // (SEC-001) is rejected here, BEFORE route type is derived or anything is persisted.
        var snapshotErrors = _validator.ValidateResolvedFlight(
            request, flightResolved, resolvedBaseFare, resolvedPricePerPassenger, resolvedOrigin, resolvedDestination);
        if (snapshotErrors.Count > 0)
        {
            throw new BookingValidationException(snapshotErrors);
        }

        // Step 3 (BR-003, DP-016, NFR-DATA-004): route type is derived from the SERVER-resolved
        // origin/destination (both guaranteed non-null here — flightResolved was true and the
        // submitted route matched), never from client fields. This closes the passport-gate
        // bypass: an international flight can no longer be booked/recorded as domestic.
        var routeType = _routeTypeResolver.Resolve(resolvedOrigin, resolvedDestination);

        // Step 4: document type/number validation against the resolved route type.
        var documentErrors = _validator.ValidateDocuments(request, routeType);
        if (documentErrors.Count > 0)
        {
            throw new BookingValidationException(documentErrors);
        }

        // Step 5 (BR-006, NFR-DATA-002): server-side total price recomputation from the
        // server-resolved per-passenger price (resolvedPricePerPassenger), not the
        // client-submitted value — the validation above already guarantees the two are
        // identical whenever this line is reached, but computing from the resolved value
        // keeps the authoritative source unambiguous (mirrors RouteTypeResolver's pattern).
        var totalPrice = Math.Round(
            resolvedPricePerPassenger * request.PassengerCount,
            2,
            MidpointRounding.AwayFromZero);

        var tenantId = _tenantContext.TenantId;

        // Step 6/7 (BR-004, NFR-DATA-001, Gap-fill BF-03, code review finding CR-003): generate
        // a candidate reference and attempt to persist it, bounded retry. CreateAsync's atomic
        // TryAdd (not the preceding ExistsAsync pre-check alone) is the actual source of truth
        // for uniqueness — a DuplicateBookingReferenceException triggers a retry with a freshly
        // generated candidate, closing the check-then-act TOCTOU race that existed when
        // uniqueness was enforced only by a separate ExistsAsync call ahead of CreateAsync.
        var created = await CreateBookingWithUniqueReferenceAsync(
            routeType, tenantId, request, totalPrice, resolvedPricePerPassenger,
            resolvedOrigin!, resolvedDestination!, cancellationToken);

        _logger.LogInformation("Booking record created for reference {BookingReference}", created.BookingReference);

        // Step 7 (FR-044): map to the response contract.
        return MapToResponse(created);
    }

    private async Task<Booking> CreateBookingWithUniqueReferenceAsync(
        RouteType routeType,
        string tenantId,
        BookingRequest request,
        decimal totalPrice,
        decimal resolvedPricePerPassenger,
        string resolvedOrigin,
        string resolvedDestination,
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
                    // AUD-025/028/033: persist the server-RESOLVED route, not the client-submitted
                    // Origin/Destination. ValidateResolvedFlight already guaranteed the two match
                    // (case-insensitively) whenever this line is reached; storing the canonical
                    // resolved values keeps the persisted record's authoritative source
                    // unambiguous, mirroring how PricePerPassenger is handled below.
                    Origin = resolvedOrigin,
                    Destination = resolvedDestination,
                    DepartureDateTime = request.Flight.DepartureDateTime!.Value,
                    ArrivalDateTime = request.Flight.ArrivalDateTime!.Value,
                    CabinClass = request.Flight.CabinClass!,
                    // SEC-001: persist the server-resolved fare, not the client-submitted one —
                    // guaranteed identical to request.Flight.PricePerPassenger whenever this line
                    // is reached (the ValidateFare check in CreateBookingAsync already enforced
                    // the match), but using the resolved value keeps the stored record's
                    // authoritative source unambiguous, mirroring RouteTypeResolver's pattern.
                    PricePerPassenger = resolvedPricePerPassenger,
                },
                Passengers = request.Passengers.Select(p => new PassengerDetail
                {
                    FullName = p.FullName!,
                    // Non-null and 0–120 guaranteed by ValidateStructure (run in
                    // BookingController before this service), same contract as FullName above.
                    Age = p.Age!.Value,
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
        Passengers = booking.Passengers.Select(p => new PassengerNameResponse { FullName = p.FullName, Age = p.Age }).ToList(),
        CreatedAtUtc = booking.CreatedAtUtc,
    };
}
