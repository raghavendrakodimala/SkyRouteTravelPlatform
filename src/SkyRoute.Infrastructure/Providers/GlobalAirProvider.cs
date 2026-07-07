using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Mock GlobalAir provider (FR-048, ASM-006, ASM-007). Fixed hardcoded schedule of 4
/// flights per feature-provider-aggregation.md Section 3.1 — the same schedule is returned
/// regardless of requested origin/destination/date (ASM-006); only the calendar date
/// component of the returned timestamps reflects the requested departureDate (Gap-fill
/// PA-03), and only cabinClass affects the returned fare (BR-009).
/// </summary>
public sealed class GlobalAirProvider : IFlightProvider
{
    public string ProviderName => "GlobalAir";

    private static readonly IReadOnlyList<ProviderScheduleMapper.ScheduledFlight> Schedule = new List<ProviderScheduleMapper.ScheduledFlight>
    {
        new("GA101", "LHR", "JFK", new TimeOnly(9, 0), 510, 250.00m),
        new("GA204", "LHR", "DXB", new TimeOnly(22, 0), 450, 300.00m),
        new("GA309", "JFK", "LAX", new TimeOnly(7, 15), 330, 180.00m),
        new("GA412", "MAN", "LHR", new TimeOnly(6, 45), 70, 80.00m),
    };

    public Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var results = ProviderScheduleMapper.BuildResults(Schedule, request, ProviderName, ApplyGlobalAirPricing);

        return Task.FromResult(results);
    }

    /// <summary>
    /// BR-001: final per-passenger price = base fare x 1.15, rounded to 2dp. Named, isolated
    /// method (DP-006, DP-019) — independently unit-testable given only a decimal base fare.
    /// </summary>
    private static decimal ApplyGlobalAirPricing(decimal baseFare) =>
        Math.Round(baseFare * 1.15m, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// SEC-001: re-derives the fare for a known flight number/cabin class from this
    /// provider's own fixed schedule, applying the exact same cabin multiplier and
    /// <see cref="ApplyGlobalAirPricing"/> rule <see cref="SearchAsync"/> uses. Departure
    /// date does not influence price for this provider (only the calendar date of the
    /// returned timestamps does), so it is intentionally not a parameter here.
    /// </summary>
    public bool TryResolveFare(string flightNumber, string cabinClass, out decimal baseFare, out decimal pricePerPassenger)
    {
        var flight = Schedule.FirstOrDefault(f => string.Equals(f.FlightNumber, flightNumber, StringComparison.Ordinal));
        if (flight is null)
        {
            baseFare = 0m;
            pricePerPassenger = 0m;
            return false;
        }

        var cabinMultiplier = CabinClassMultipliers.ForCabinClass(cabinClass);
        baseFare = Math.Round(flight.EconomyBaseFare * cabinMultiplier, 2, MidpointRounding.AwayFromZero);
        pricePerPassenger = ApplyGlobalAirPricing(baseFare);
        return true;
    }
}
