using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Mock BudgetWings provider (FR-048, ASM-006, ASM-007). Fixed hardcoded schedule of 4
/// flights per feature-provider-aggregation.md Section 3.2.
/// </summary>
public sealed class BudgetWingsProvider : IFlightProvider
{
    public string ProviderName => "BudgetWings";

    private static readonly IReadOnlyList<ProviderScheduleMapper.ScheduledFlight> Schedule = new List<ProviderScheduleMapper.ScheduledFlight>
    {
        new("BW210", "LHR", "JFK", new TimeOnly(11, 0), 495, 220.00m),
        new("BW225", "SYD", "LAX", new TimeOnly(23, 0), 780, 450.00m),
        new("BW238", "LAX", "JFK", new TimeOnly(6, 0), 300, 150.00m),
        new("BW241", "MAN", "LHR", new TimeOnly(14, 0), 65, 60.00m),
    };

    public Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var results = ProviderScheduleMapper.BuildResults(Schedule, request, ProviderName, ApplyBudgetWingsPricing);

        return Task.FromResult(results);
    }

    /// <summary>
    /// BR-002: final per-passenger price = max(base fare x 0.90, 29.99), rounded to 2dp
    /// before the floor is applied. Named, isolated method (DP-006, DP-019) — independently
    /// unit-testable given only a decimal base fare.
    /// </summary>
    private static decimal ApplyBudgetWingsPricing(decimal baseFare)
    {
        var discounted = Math.Round(baseFare * 0.90m, 2, MidpointRounding.AwayFromZero);
        return Math.Max(discounted, 29.99m);
    }
}
