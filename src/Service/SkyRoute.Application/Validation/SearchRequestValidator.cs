using System.Text.RegularExpressions;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Data;

namespace SkyRoute.Application.Validation;

/// <summary>
/// Cross-field/context validation for POST /api/search (AD-003, BL-010). This class is the
/// single authoritative source of the exact field-keyed error messages returned to the
/// client (feature-flight-search.md Section 4.1) — the backend's automatic [ApiController]
/// model-state filter is suppressed (Program.cs) precisely so this validator's messages,
/// not framework defaults, are what clients ever see. Does not short-circuit after the
/// first failure — all failing fields are reported together (FR-063).
/// </summary>
public sealed class SearchRequestValidator
{
    private static readonly Regex AirportCodeFormat = new("^[A-Z]{3}$", RegexOptions.Compiled);

    private readonly AirportDataService _airportDataService;

    public SearchRequestValidator(AirportDataService airportDataService)
    {
        _airportDataService = airportDataService;
    }

    public IDictionary<string, string[]> Validate(SearchRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        ValidateOrigin(request, errors);
        ValidateDestination(request, errors);
        ValidateDepartureDate(request, errors);
        ValidatePassengerCount(request, errors);
        ValidateCabinClass(request, errors);
        ValidateTripType(request, errors);

        return errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }

    private void ValidateOrigin(SearchRequest request, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Origin) || !AirportCodeFormat.IsMatch(request.Origin))
        {
            AddError(errors, "origin", "Origin airport code is required and must be a valid 3-letter airport code.");
        }
        else if (!_airportDataService.IsKnownAirportCode(request.Origin))
        {
            AddError(errors, "origin", "Origin airport code is not recognized.");
        }
    }

    private void ValidateDestination(SearchRequest request, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Destination) || !AirportCodeFormat.IsMatch(request.Destination))
        {
            AddError(errors, "destination", "Destination airport code is required and must be a valid 3-letter airport code.");
            return;
        }

        if (!_airportDataService.IsKnownAirportCode(request.Destination))
        {
            AddError(errors, "destination", "Destination airport code is not recognized.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(request.Origin) &&
            AirportCodeFormat.IsMatch(request.Origin) &&
            _airportDataService.IsKnownAirportCode(request.Origin) &&
            string.Equals(request.Origin, request.Destination, StringComparison.Ordinal))
        {
            AddError(errors, "destination", "Origin and destination airports must be different.");
        }
    }

    private static void ValidateDepartureDate(SearchRequest request, Dictionary<string, List<string>> errors)
    {
        if (request.DepartureDate is null)
        {
            AddError(errors, "departureDate", "Departure date is required and must be a valid date.");
            return;
        }

        // AUD-026/031: compare against the timezone-generous boundary (UTC today − 1 day) rather
        // than raw UTC "today", so a negative-offset user's legitimate same-day local search is
        // not wrongly rejected once UTC has rolled to the next day. See DepartureDateRules.
        if (request.DepartureDate.Value < DepartureDateRules.EarliestAcceptableDateUtc())
        {
            AddError(errors, "departureDate", "Departure date cannot be in the past.");
        }
    }

    private static void ValidatePassengerCount(SearchRequest request, Dictionary<string, List<string>> errors)
    {
        if (request.PassengerCount < 1 || request.PassengerCount > 9)
        {
            AddError(errors, "passengerCount", "Passenger count must be a whole number between 1 and 9.");
        }
    }

    private static void ValidateCabinClass(SearchRequest request, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(request.CabinClass) || !CabinClasses.ValidCabinClasses.Contains(request.CabinClass))
        {
            AddError(errors, "cabinClass", "Cabin class must be one of: Economy, Business, First Class.");
        }
    }

    private static void ValidateTripType(SearchRequest request, Dictionary<string, List<string>> errors)
    {
        if (!string.Equals(request.TripType, "OneWay", StringComparison.Ordinal))
        {
            AddError(errors, "tripType", "Trip type must be 'OneWay'.");
        }
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = [];
            errors[field] = list;
        }

        list.Add(message);
    }
}
