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

        if (request.PassengerCount != passengers.Count)
        {
            AddError(errors, "passengerCount", "Passenger count must match the number of passenger records submitted.");
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
