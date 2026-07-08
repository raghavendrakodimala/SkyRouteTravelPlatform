using SkyRoute.Application.Dtos;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Mock GlobalAir provider (FR-048, ASM-006, ASM-007). Schedule = the 4 fixed fixture
/// flights per feature-provider-aggregation.md Section 3.1 (returned exactly as documented
/// on their routes) plus deterministic generated flights for every ordered airport pair the
/// fixtures do not cover (<see cref="RouteScheduleGenerator"/> — PO defect fix, 2026-07-07:
/// any valid route now returns results). Only the calendar date component of the returned
/// timestamps reflects the requested departureDate (Gap-fill PA-03), and only cabinClass
/// affects the returned fare (BR-009); the requested date never filters the schedule.
/// </summary>
public sealed class GlobalAirProvider : IFlightProvider
{
    public string ProviderName => "GlobalAir";

    /// <summary>Authoritative fixtures — feature-provider-aggregation.md Section 3.1. The
    /// entire test suite pins these; they must not change. Declared before
    /// <see cref="Schedule"/> (static initializer order matters).</summary>
    private static readonly IReadOnlyList<ProviderScheduleMapper.ScheduledFlight> FixedSchedule =
    [
        new("GA101", "LHR", "JFK", new TimeOnly(9, 0), 510, 250.00m),
        new("GA204", "LHR", "DXB", new TimeOnly(22, 0), 450, 300.00m),
        new("GA309", "JFK", "LAX", new TimeOnly(7, 15), 330, 180.00m),
        new("GA412", "MAN", "LHR", new TimeOnly(6, 45), 70, 80.00m),
    ];

    /// <summary>Full schedule: fixtures + deterministic generated flights (departing 07:30
    /// and 16:45) for every route the fixtures don't cover. Computed once — identical
    /// searches always return identical flights.</summary>
    private static readonly IReadOnlyList<ProviderScheduleMapper.ScheduledFlight> Schedule =
        RouteScheduleGenerator.BuildFullSchedule(
            FixedSchedule,
            flightNumberPrefix: "GA",
            firstDepartureTimeOfDay: new TimeOnly(7, 30),
            secondDepartureTimeOfDay: new TimeOnly(16, 45),
            baseFareForDuration: GeneratedRouteBaseFare);

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
    /// Economy base fare for a GENERATED route (fixtures keep their own documented fares):
    /// round(60 + durationMinutes × 0.55, 2). Deterministic and duration-derived, so longer
    /// routes cost more; BR-001 (+15%, <see cref="ApplyGlobalAirPricing"/>) and the cabin
    /// multipliers then apply unchanged through the existing pipeline.
    /// </summary>
    private static decimal GeneratedRouteBaseFare(int durationMinutes) =>
        Math.Round(60m + (durationMinutes * 0.55m), 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// SEC-001 + AUD-025/028/033: from ONE schedule lookup for a known flight number/cabin
    /// class (fixtures + generated routes — a booking made on a generated flight must resolve
    /// exactly like a fixture one) surfaces both the re-derived fare (same cabin multiplier and
    /// <see cref="ApplyGlobalAirPricing"/> rule <see cref="SearchAsync"/> uses) and the flight's
    /// authoritative <paramref name="origin"/>/<paramref name="destination"/>. Departure date
    /// does not influence price for this provider (only the calendar date of the returned
    /// timestamps does), so it is intentionally not a parameter here.
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
        pricePerPassenger = ApplyGlobalAirPricing(baseFare);
        origin = flight.Origin;
        destination = flight.Destination;
        return true;
    }
}
