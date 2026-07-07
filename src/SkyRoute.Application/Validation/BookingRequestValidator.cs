using System.Text.RegularExpressions;
using SkyRoute.Application.Contracts;
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
    private static readonly Regex FullNameRegex = new(DocumentPatterns.FullNamePattern, RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(DocumentPatterns.EmailPattern, RegexOptions.Compiled);
    private static readonly Regex PassportRegex = new(DocumentPatterns.PassportPattern, RegexOptions.Compiled);
    private static readonly Regex NationalIdRegex = new(DocumentPatterns.NationalIdPattern, RegexOptions.Compiled);

    public IDictionary<string, string[]> ValidateStructure(BookingRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        // Defensive against a malformed body that explicitly sends "flight": null or
        // "passengers": null (overriding the property initializer default) — treated as a
        // 400 validation error rather than an unhandled exception (BR-011).
        var passengers = request.Passengers ?? new List<PassengerRequest>();

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
        }

        if (request.PassengerCount != passengers.Count)
        {
            AddError(errors, "passengerCount", "Passenger count must match the number of passenger records submitted.");
        }

        // SEC-002 (Phase 16 security review): mirrors SearchRequestValidator.ValidatePassengerCount
        // exactly — same 1-9 bound, same error message — so the booking endpoint enforces the same
        // upper bound the search endpoint already enforces, rather than relying solely on the
        // Angular UI never allowing more than 9 passengers in practice.
        if (request.PassengerCount < 1 || request.PassengerCount > 9)
        {
            AddError(errors, "passengerCount", "Passenger count must be a whole number between 1 and 9.");
        }

        for (var i = 0; i < passengers.Count; i++)
        {
            var passenger = passengers[i];

            if (string.IsNullOrWhiteSpace(passenger.FullName) || !FullNameRegex.IsMatch(passenger.FullName))
            {
                AddError(errors, $"passengers[{i}].fullName",
                    "Full name is required, must be 2–100 characters, and must contain at least one letter.");
            }

            if (string.IsNullOrWhiteSpace(passenger.Email) || !EmailRegex.IsMatch(passenger.Email))
            {
                AddError(errors, $"passengers[{i}].email", "A valid email address is required.");
            }
        }

        return ToArrayDictionary(errors);
    }

    /// <summary>
    /// SEC-001 (Phase 16 security review, BR-006, NFR-DATA-002) — step 2b: validates the
    /// client-submitted flight-fare snapshot against the fare BookingService's
    /// FlightFareResolver has already authoritatively re-derived server-side from the same
    /// provider pricing logic used at search time. Mirrors <see cref="ValidateDocuments"/>'s
    /// shape (resolution happens in the service, this method only validates the resolved
    /// fact against the request) — invoked by BookingService after RouteTypeResolver and
    /// FlightFareResolver have both run, before document validation is relied upon to gate
    /// price computation. Pricing is deterministic given Provider+FlightNumber+CabinClass, so
    /// an exact match is required — there is no legitimate reason for the two values to
    /// differ even by a rounding cent, since both sides use the identical rounding rule.
    /// </summary>
    public IDictionary<string, string[]> ValidateFare(
        BookingRequest request, bool fareResolved, decimal expectedBaseFare, decimal expectedPricePerPassenger)
    {
        var errors = new Dictionary<string, List<string>>();

        if (request.Flight is null)
        {
            // ValidateStructure already reports "flight" as incomplete; nothing further to
            // check here without a flight snapshot to compare against.
            return ToArrayDictionary(errors);
        }

        if (!fareResolved)
        {
            AddError(errors, "flight.flightNumber",
                "Flight could not be verified against the selected provider's published schedule.");
            return ToArrayDictionary(errors);
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

        var expectedDocumentType = routeType == RouteType.International
            ? DocumentPatterns.PassportDocumentType
            : DocumentPatterns.NationalIdDocumentType;

        var pattern = routeType == RouteType.International ? PassportRegex : NationalIdRegex;
        var patternErrorMessage = routeType == RouteType.International
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
            list = new List<string>();
            errors[field] = list;
        }

        list.Add(message);
    }

    private static IDictionary<string, string[]> ToArrayDictionary(Dictionary<string, List<string>> errors) =>
        errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
}
