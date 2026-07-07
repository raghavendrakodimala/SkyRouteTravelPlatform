using SkyRoute.Application.Data;
using SkyRoute.Application.Domain;

namespace SkyRoute.Application.Services;

/// <summary>
/// Single named location for domestic/international route-type determination (BR-003, DP-016).
/// Authoritative — re-evaluated server-side at booking time from the flight snapshot's
/// origin/destination, independent of any client-submitted route type or document type.
/// </summary>
public sealed class RouteTypeResolver
{
    private readonly AirportDataService _airportDataService;

    public RouteTypeResolver(AirportDataService airportDataService)
    {
        _airportDataService = airportDataService;
    }

    /// <summary>
    /// Resolves route type from origin/destination airport codes (BR-003: countries equal
    /// => Domestic, countries differ => International). If either code is not a known
    /// airport in this layer's static list, defaults to International (the stricter,
    /// safer document-format requirement) — this edge case is not addressed by any
    /// approved requirement and is not expected to occur in the MVP's fixed-mock-data flow.
    /// </summary>
    public RouteType Resolve(string? originCode, string? destinationCode)
    {
        var originCountry = _airportDataService.GetCountryOrNull(originCode);
        var destinationCountry = _airportDataService.GetCountryOrNull(destinationCode);

        if (originCountry is null || destinationCountry is null)
        {
            return RouteType.International;
        }

        return string.Equals(originCountry, destinationCountry, StringComparison.OrdinalIgnoreCase)
            ? RouteType.Domestic
            : RouteType.International;
    }
}
