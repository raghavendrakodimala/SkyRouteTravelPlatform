using System.Text.RegularExpressions;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Data;
using SkyRoute.Application.Domain;

namespace SkyRoute.Application.Validation;

/// <summary>
/// Validation for POST /api/bookings (AD-003, BL-014), split into two methods matching
/// architecture-plan.md Section 3.3's booking flow:
/// - <see cref="ValidateStructure"/> — step 1: structural checks (passenger count vs. array
///   length, flight snapshot completeness, full name, email) that do not depend on the
///   server-resolved route type. Invoked by BookingController before calling IBookingService.
/// - <see cref="ValidateDocuments"/> — step 3: document type/number checks against the
///   *server-resolved* route type (BR-003, DP-016). Invoked by BookingService after
///   RouteTypeResolver has run, since the applicable pattern depends on that result.
/// Neither method short-circuits after the first failure (FR-063).
/// </summary>
public sealed class BookingRequestValidator
{
    /// <summary>Passenger age sanity bounds (PO age feature 2026-07-08; DEC-022: age is pure
    /// data capture — no business rule is bound to it) — mirrored (not duplicated) by
    /// AGE_MIN/AGE_MAX in the Angular document-number.validators.ts (DP-015 spirit).</summary>
    private const int MinAge = 0;
    private const int MaxAge = 120;

    /// <summary>Maximum passengers per booking (SEC-002 / AUD-034) — mirrors the 1–9 bound the
    /// search endpoint enforces.</summary>
    private const int MaxPassengers = 9;

    private static readonly Regex FullNameRegex = new(DocumentPatterns.FullNamePattern, RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(DocumentPatterns.EmailPattern, RegexOptions.Compiled);
    private static readonly Regex PassportRegex = new(DocumentPatterns.PassportPattern, RegexOptions.Compiled);
    private static readonly Regex NationalIdRegex = new(DocumentPatterns.NationalIdPattern, RegexOptions.Compiled);

    private readonly AirportDataService _airportDataService;

    public BookingRequestValidator(AirportDataService airportDataService)
    {
        _airportDataService = airportDataService;
    }

    public IDictionary<string, string[]> ValidateStructure(BookingRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        // Defensive against a malformed body that explicitly sends "flight": null or
        // "passengers": null (overriding the property initializer default) — treated as a
        // 400 validation error rather than an unhandled exception (BR-011).
        var passengers = request.Passengers ?? [];

        if (request.Flight is null || !IsFlightSnapshotComplete(request.Flight))
        {
            AddError(errors, "flight", "Flight details are incomplete.");
        }

        // SEC-001 (Phase 16 security review): the flight snapshot fields above are only
        // checked for *presence*, not correctness — a client can submit a zero/negative
        // PricePerPassenger/BaseFare or an arbitrary CabinClass string and have it flow
        // straight into BookingService's total-price computation and persisted record.
        // These checks do not short-circuit on flight-completeness (FR-063) so a partially
        // complete flight snapshot still reports every applicable structural problem.
        if (request.Flight is not null)
        {
            if (request.Flight.PricePerPassenger is <= 0)
            {
                AddError(errors, "flight.pricePerPassenger", "Price per passenger must be greater than zero.");
            }

            if (request.Flight.BaseFare is <= 0)
            {
                AddError(errors, "flight.baseFare", "Base fare must be greater than zero.");
            }

            if (!string.IsNullOrWhiteSpace(request.Flight.CabinClass) &&
                !CabinClasses.ValidCabinClasses.Contains(request.Flight.CabinClass))
            {
                AddError(errors, "flight.cabinClass", "Cabin class must be one of: Economy, Business, First Class.");
            }

            // AUD-029: the flight snapshot was previously presence-only validated, so unknown
            // airport codes, arrival-before-departure, and already-past departures were persisted
            // verbatim. Mirror the search-side sanity rules here. Each check only fires when its
            // field is actually present — a missing field is already reported by the
            // "flight incomplete" check above (FR-063: no short-circuit, every problem reported).
            if (!string.IsNullOrWhiteSpace(request.Flight.Origin) &&
                !_airportDataService.IsKnownAirportCode(request.Flight.Origin))
            {
                AddError(errors, "flight.origin", "Origin airport code is not recognized.");
            }

            if (!string.IsNullOrWhiteSpace(request.Flight.Destination) &&
                !_airportDataService.IsKnownAirportCode(request.Flight.Destination))
            {
                AddError(errors, "flight.destination", "Destination airport code is not recognized.");
            }

            if (request.Flight.DepartureDateTime.HasValue &&
                request.Flight.ArrivalDateTime.HasValue &&
                request.Flight.ArrivalDateTime.Value <= request.Flight.DepartureDateTime.Value)
            {
                AddError(errors, "flight.arrivalDateTime", "Arrival must be after departure.");
            }

            // AUD-026/029: reject a departure whose calendar date is already in the past, using
            // the same timezone-generous boundary the search endpoint uses (DepartureDateRules).
            if (request.Flight.DepartureDateTime.HasValue &&
                DateOnly.FromDateTime(request.Flight.DepartureDateTime.Value) < DepartureDateRules.EarliestAcceptableDateUtc())
            {
                AddError(errors, "flight.departureDateTime", "Departure cannot be in the past.");
            }
        }

        if (request.PassengerCount != passengers.Count)
        {
            AddError(errors, "passengerCount", "Passenger count must match the number of passenger records submitted.");
        }

        // SEC-002 (Phase 16 security review): mirrors SearchRequestValidator.ValidatePassengerCount
        // exactly — same 1-9 bound, same error message — so the booking endpoint enforces the same
        // upper bound the search endpoint already enforces, rather than relying solely on the
        // Angular UI never allowing more than 9 passengers in practice.
        if (request.PassengerCount < 1 || request.PassengerCount > MaxPassengers)
        {
            AddError(errors, "passengerCount", "Passenger count must be a whole number between 1 and 9.");
        }

        // AUD-034 (OWASP API4:2023 Unrestricted Resource Consumption / CWE-770): bound the
        // passengers ARRAY itself, not just the scalar passengerCount, BEFORE the per-element
        // loop. SEC-002 bounded the scalar count, but an attacker could still send a small
        // passengerCount alongside a multi-million-element passengers[] — the count checks pass
        // (or append a non-returning mismatch error) and the loop below still walks the whole
        // oversized array, amplifying one ~28 MB request into millions of dictionary allocations
        // before the 400 is produced. If the array exceeds the maximum, report and return
        // immediately so the loop is never entered.
        if (passengers.Count > MaxPassengers)
        {
            AddError(errors, "passengers", "A booking may include at most 9 passengers.");
            return ToArrayDictionary(errors);
        }

        for (var i = 0; i < passengers.Count; i++)
        {
            var passenger = passengers[i];

            if (string.IsNullOrWhiteSpace(passenger.FullName) || !FullNameRegex.IsMatch(passenger.FullName))
            {
                AddError(errors, $"passengers[{i}].fullName",
                    "Full name is required, must be 2–100 characters, and must contain at least one letter.");
            }

            // Age (PO age feature 2026-07-08): required whole number 0–120 per passenger;
            // JSON model binding already rejects non-integer values, so only presence and
            // range are checked here. DEC-022 (PO 2026-07-08): these are sanity bounds only —
            // age is pure data capture and no business rule (pricing, eligibility, lead-adult)
            // is bound to it.
            if (passenger.Age is null || passenger.Age < MinAge || passenger.Age > MaxAge)
            {
                AddError(errors, $"passengers[{i}].age",
                    "Age is required and must be a whole number between 0 and 120.");
            }

            if (string.IsNullOrWhiteSpace(passenger.Email) || !EmailRegex.IsMatch(passenger.Email))
            {
                AddError(errors, $"passengers[{i}].email", "A valid email address is required.");
            }
        }

        return ToArrayDictionary(errors);
    }

    /// <summary>
    /// SEC-001 (Phase 16 security review, BR-006, NFR-DATA-002) and AUD-025/028/033 — step 2b:
    /// validates the client-submitted identifying flight snapshot (route AND fare) against the
    /// authoritative values BookingService's FlightFareResolver has already re-resolved
    /// server-side from the provider's published schedule, keyed on Provider+FlightNumber+
    /// CabinClass. Mirrors <see cref="ValidateDocuments"/>'s shape (resolution happens in the
    /// service; this method only validates the resolved facts against the request) — invoked by
    /// BookingService BEFORE route type is derived, so a forged Origin/Destination (e.g. an
    /// international flight declared as a domestic pair to dodge BR-003's passport gate) is
    /// rejected and never reaches route-type resolution or persistence. Route match is
    /// case-insensitive (the airport-code casing convention throughout this codebase); fare
    /// match is exact — pricing is deterministic given these three fields, so there is no
    /// legitimate reason for the values to differ even by a rounding cent.
    /// </summary>
    public IDictionary<string, string[]> ValidateResolvedFlight(
        BookingRequest request,
        bool flightResolved,
        decimal expectedBaseFare,
        decimal expectedPricePerPassenger,
        string? resolvedOrigin,
        string? resolvedDestination)
    {
        var errors = new Dictionary<string, List<string>>();

        if (request.Flight is null)
        {
            // ValidateStructure already reports "flight" as incomplete; nothing further to
            // check here without a flight snapshot to compare against.
            return ToArrayDictionary(errors);
        }

        if (!flightResolved)
        {
            AddError(errors, "flight.flightNumber",
                "Flight could not be verified against the selected provider's published schedule.");
            return ToArrayDictionary(errors);
        }

        // AUD-025/028/033: the resolved flight's route is authoritative. A submitted Origin/
        // Destination that does not match it is a forged route — reject it here so it can never
        // drive route-type resolution (BR-003) or be persisted.
        if (!string.Equals(request.Flight.Origin, resolvedOrigin, StringComparison.OrdinalIgnoreCase))
        {
            AddError(errors, "flight.origin",
                "Origin does not match the provider's published route for this flight.");
        }

        if (!string.Equals(request.Flight.Destination, resolvedDestination, StringComparison.OrdinalIgnoreCase))
        {
            AddError(errors, "flight.destination",
                "Destination does not match the provider's published route for this flight.");
        }

        if (request.Flight.PricePerPassenger.HasValue &&
            request.Flight.PricePerPassenger.Value != expectedPricePerPassenger)
        {
            AddError(errors, "flight.pricePerPassenger",
                "Price per passenger does not match the provider's published fare for this flight and cabin class.");
        }

        if (request.Flight.BaseFare.HasValue && request.Flight.BaseFare.Value != expectedBaseFare)
        {
            AddError(errors, "flight.baseFare",
                "Base fare does not match the provider's published fare for this flight and cabin class.");
        }

        return ToArrayDictionary(errors);
    }

    public IDictionary<string, string[]> ValidateDocuments(BookingRequest request, RouteType routeType)
    {
        var errors = new Dictionary<string, List<string>>();

        var expectedDocumentType = routeType.IsInternational
            ? DocumentPatterns.PassportDocumentType
            : DocumentPatterns.NationalIdDocumentType;

        var pattern = routeType.IsInternational ? PassportRegex : NationalIdRegex;
        var patternErrorMessage = routeType.IsInternational
            ? "Passport number must be 6–9 uppercase letters and digits, with no spaces."
            : "National ID must be 5–20 letters, digits, or hyphens, with no spaces.";

        for (var i = 0; i < request.Passengers.Count; i++)
        {
            var passenger = request.Passengers[i];

            if (!string.Equals(passenger.DocumentType, expectedDocumentType, StringComparison.Ordinal))
            {
                AddError(errors, $"passengers[{i}].documentType", "Document type does not match the route for this booking.");
            }

            if (string.IsNullOrWhiteSpace(passenger.DocumentNumber) || !pattern.IsMatch(passenger.DocumentNumber))
            {
                AddError(errors, $"passengers[{i}].documentNumber", patternErrorMessage);
            }
        }

        return ToArrayDictionary(errors);
    }

    private static bool IsFlightSnapshotComplete(BookingFlightRequest flight) =>
        !string.IsNullOrWhiteSpace(flight.Provider) &&
        !string.IsNullOrWhiteSpace(flight.FlightNumber) &&
        !string.IsNullOrWhiteSpace(flight.Origin) &&
        !string.IsNullOrWhiteSpace(flight.Destination) &&
        flight.DepartureDateTime.HasValue &&
        flight.ArrivalDateTime.HasValue &&
        !string.IsNullOrWhiteSpace(flight.CabinClass) &&
        flight.PricePerPassenger.HasValue;

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = [];
            errors[field] = list;
        }

        list.Add(message);
    }

    private static IDictionary<string, string[]> ToArrayDictionary(Dictionary<string, List<string>> errors) =>
        errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
}
