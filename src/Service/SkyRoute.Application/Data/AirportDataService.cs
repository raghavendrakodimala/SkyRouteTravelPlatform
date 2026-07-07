using SkyRoute.Application.Domain;

namespace SkyRoute.Application.Data;

/// <summary>
/// Backend's independent, static copy of the airport list, used for origin/destination
/// validation (FR-006) and route-type resolution (BR-003). Concrete class, no interface,
/// per requirements.md's explicit YAGNI-001 note — this is a static in-process constant,
/// not a persistence concern. This is a separate authoritative source for this layer only;
/// the Angular frontend maintains its own equivalent constant (FR-055).
/// </summary>
public sealed class AirportDataService
{
    private static readonly IReadOnlyList<Airport> Airports = new List<Airport>
    {
        new() { Code = "LHR", City = "London", Country = "United Kingdom", DisplayName = "London Heathrow (LHR)" },
        new() { Code = "MAN", City = "Manchester", Country = "United Kingdom", DisplayName = "Manchester (MAN)" },
        new() { Code = "JFK", City = "New York", Country = "United States", DisplayName = "New York JFK (JFK)" },
        new() { Code = "LAX", City = "Los Angeles", Country = "United States", DisplayName = "Los Angeles (LAX)" },
        new() { Code = "DXB", City = "Dubai", Country = "United Arab Emirates", DisplayName = "Dubai (DXB)" },
        new() { Code = "SYD", City = "Sydney", Country = "Australia", DisplayName = "Sydney (SYD)" },
    };

    public IReadOnlyList<Airport> GetAll() => Airports;

    public bool IsKnownAirportCode(string? code) =>
        !string.IsNullOrEmpty(code) &&
        Airports.Any(a => string.Equals(a.Code, code, StringComparison.Ordinal));

    public string? GetCountryOrNull(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        return Airports.FirstOrDefault(a => string.Equals(a.Code, code, StringComparison.Ordinal))?.Country;
    }
}
