using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Mock BudgetWings provider (FR-048, ASM-006, ASM-007). Schedule = the 4 fixed fixture
/// flights per feature-provider-aggregation.md Section 3.2 (returned exactly as documented
/// on their routes) plus deterministic generated flights for every ordered airport pair the
/// fixtures do not cover (<see cref="RouteScheduleGenerator"/> — PO defect fix, 2026-07-07:
/// any valid route now returns results).
/// </summary>
public sealed class BudgetWingsProvider : IFlightProvider
{
    public string ProviderName => "BudgetWings";

    /// <summary>Authoritative fixtures — feature-provider-aggregation.md Section 3.2. The
    /// entire test suite pins these; they must not change. Declared before
    /// <see cref="Schedule"/> (static initializer order matters).</summary>
    private static readonly IReadOnlyList<ProviderScheduleMapper.ScheduledFlight> FixedSchedule =
    [
        new("BW210", "LHR", "JFK", new TimeOnly(11, 0), 495, 220.00m),
        new("BW225", "SYD", "LAX", new TimeOnly(23, 0), 780, 450.00m),
        new("BW238", "LAX", "JFK", new TimeOnly(6, 0), 300, 150.00m),
        new("BW241", "MAN", "LHR", new TimeOnly(14, 0), 65, 60.00m),
    ];

    /// <summary>Full schedule: fixtures + deterministic generated flights (departing 10:15
    /// and 21:30) for every route the fixtures don't cover. Computed once — identical
    /// searches always return identical flights.</summary>
    private static readonly IReadOnlyList<ProviderScheduleMapper.ScheduledFlight> Schedule =
        RouteScheduleGenerator.BuildFullSchedule(
            FixedSchedule,
            flightNumberPrefix: "BW",
            firstDepartureTimeOfDay: new TimeOnly(10, 15),
            secondDepartureTimeOfDay: new TimeOnly(21, 30),
            baseFareForDuration: GeneratedRouteBaseFare);

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

    /// <summary>
    /// Economy base fare for a GENERATED route (fixtures keep their own documented fares):
    /// round(45 + durationMinutes × 0.42, 2). Deterministic and duration-derived — cheaper
    /// per minute than GlobalAir, consistent with the budget-carrier positioning; BR-002
    /// (−10% with the 29.99 floor, <see cref="ApplyBudgetWingsPricing"/>) and the cabin
    /// multipliers then apply unchanged through the existing pipeline.
    /// </summary>
    private static decimal GeneratedRouteBaseFare(int durationMinutes) =>
        Math.Round(45m + (durationMinutes * 0.42m), 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// SEC-001 + AUD-025/028/033: from ONE schedule lookup for a known flight number/cabin
    /// class (fixtures + generated routes — a booking made on a generated flight must resolve
    /// exactly like a fixture one) surfaces both the re-derived fare (same cabin multiplier and
    /// <see cref="ApplyBudgetWingsPricing"/> rule <see cref="SearchAsync"/> uses) and the
    /// flight's authoritative <paramref name="origin"/>/<paramref name="destination"/>.
    /// Departure date does not influence price for this provider (only the calendar date of the
    /// returned timestamps does), so it is intentionally not a parameter here.
    /// </summary>
    public bool TryResolveFare(
        string flightNumber,
        string cabinClass,
        out decimal baseFare,
        out decimal pricePerPassenger,
        out string? origin,
        out string? destination)
    {
        var flight = Schedule.FirstOrDefault(f => string.Equals(f.FlightNumber, flightNumber, StringComparison.Ordinal));
        if (flight is null)
        {
            baseFare = 0m;
            pricePerPassenger = 0m;
            origin = null;
            destination = null;
            return false;
        }

        var cabinMultiplier = CabinClassMultipliers.ForCabinClass(cabinClass);
        baseFare = Math.Round(flight.EconomyBaseFare * cabinMultiplier, 2, MidpointRounding.AwayFromZero);
        pricePerPassenger = ApplyBudgetWingsPricing(baseFare);
        origin = flight.Origin;
        destination = flight.Destination;
        return true;
    }
}
