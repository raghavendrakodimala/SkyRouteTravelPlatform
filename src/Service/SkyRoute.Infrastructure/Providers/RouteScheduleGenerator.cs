namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Deterministic route-coverage generator (PO defect fix, 2026-07-07: "empty state most of
/// the time"). The fixed per-provider fixtures (feature-provider-aggregation.md Section 3)
/// cover only 6 of the 30 ordered airport pairs, so most valid searches returned the empty
/// state — violating the challenge requirement of "a realistic set of flight results for any
/// given search." This class completes each provider's schedule by generating flights for
/// every ordered route in the served-pair table below that the provider's own fixed schedule
/// does NOT already cover. One pair, MAN-SYD, is deliberately absent from the table (DEC-021,
/// PO 2026-07-08 — see the table's doc comment), so the styled empty state remains
/// demonstrable through a real search.
///
/// Guarantees:
/// - Pure function of its inputs — NO randomness, NO clock reads. The same provider always
///   exposes the exact same full schedule, so identical searches always return identical
///   flights (schedules are computed once into each provider's static schedule field).
/// - Fixture preservation — a route present in the provider's fixed schedule is skipped
///   entirely, so fixture routes keep returning exactly their documented flights and prices.
/// - Collision-free flight numbers — generated numbers occupy 500–555 (fixtures use
///   101–412), and every ordered route gets its own distinct pair of numbers:
///   number = 500 + pairIndex*4 + directionOffset(0|2) + flightIndex(0|1), where pairIndex
///   is the route's position in the 14-row unordered-pair table below and directionOffset
///   distinguishes A→B from B→A (the same physical city pair flown in opposite directions
///   must not share a flight number — Provider+FlightNumber is the fare lookup key, see
///   docs/architecture/data-model.md Section 1.4).
/// - Symmetric durations — both directions of a pair share the table's duration, and
///   per-provider base fares are derived from that duration by the provider-supplied
///   <c>baseFareForDuration</c> delegate, so the existing pricing rules (BR-001/BR-002) and
///   cabin multipliers apply unchanged through <see cref="ProviderScheduleMapper"/>.
/// </summary>
internal static class RouteScheduleGenerator
{
    private sealed record RoutePair(string AirportA, string AirportB, int DurationMinutes);

    /// <summary>
    /// One row per SERVED unordered pair of the 6 supported airports (docs/requirements.md
    /// Section 3.7 / AirportDataService), with a realistic symmetric flight duration in
    /// minutes. Row order is load-bearing: a route's index here feeds the flight-number
    /// formula, so rows must only ever be appended, never reordered.
    ///
    /// MAN-SYD is DELIBERATELY absent (DEC-021, PO direction 2026-07-08): it is the one
    /// no-direct-service pair — true to the real world, where no nonstop Manchester–Sydney
    /// flight exists — kept unserved so the styled empty state stays demonstrable through a
    /// real search (challenge PDF 3.2: "a clear empty state if no flights match").
    /// Generation is table-driven, so a pair absent from this table is simply skipped, never
    /// an error: both directions return no generated flights, and neither provider has a
    /// MAN-SYD/SYD-MAN fixture, so both providers genuinely return zero results for it.
    /// </summary>
    private static readonly IReadOnlyList<RoutePair> RouteDurations =
    [
        new("LHR", "MAN", 65),
        new("LHR", "JFK", 490),
        new("LHR", "LAX", 660),
        new("LHR", "DXB", 420),
        new("LHR", "SYD", 1320),
        new("MAN", "JFK", 500),
        new("MAN", "LAX", 680),
        new("MAN", "DXB", 435),
        new("JFK", "LAX", 350),
        new("JFK", "DXB", 760),
        new("JFK", "SYD", 1330),
        new("LAX", "DXB", 980),
        new("LAX", "SYD", 900),
        new("DXB", "SYD", 840),
    ];

    private const int GeneratedFlightNumberBase = 500;
    private const int NumbersPerPair = 4;      // 2 directions × 2 flights
    private const int ReverseDirectionOffset = 2;

    /// <summary>
    /// Returns the provider's complete schedule: the <paramref name="fixedSchedule"/> fixtures
    /// (first, order preserved, entirely untouched) followed by two generated flights — one at
    /// <paramref name="firstDepartureTimeOfDay"/>, one at <paramref name="secondDepartureTimeOfDay"/> —
    /// for every ordered airport pair the fixed schedule does not cover. Arrival times
    /// (including overnight rollover) are still computed by <see cref="ProviderScheduleMapper"/>
    /// exactly as they are for fixtures.
    /// </summary>
    public static IReadOnlyList<ProviderScheduleMapper.ScheduledFlight> BuildFullSchedule(
        IReadOnlyList<ProviderScheduleMapper.ScheduledFlight> fixedSchedule,
        string flightNumberPrefix,
        TimeOnly firstDepartureTimeOfDay,
        TimeOnly secondDepartureTimeOfDay,
        Func<int, decimal> baseFareForDuration)
    {
        var fullSchedule = new List<ProviderScheduleMapper.ScheduledFlight>(fixedSchedule);

        var coveredRoutes = fixedSchedule
            .Select(flight => RouteKey(flight.Origin, flight.Destination))
            .ToHashSet(StringComparer.Ordinal);

        for (var pairIndex = 0; pairIndex < RouteDurations.Count; pairIndex++)
        {
            var pair = RouteDurations[pairIndex];

            AppendRouteIfUncovered(pair.AirportA, pair.AirportB, directionOffset: 0);
            AppendRouteIfUncovered(pair.AirportB, pair.AirportA, directionOffset: ReverseDirectionOffset);

            void AppendRouteIfUncovered(string origin, string destination, int directionOffset)
            {
                if (coveredRoutes.Contains(RouteKey(origin, destination)))
                {
                    return; // fixture route — must keep returning exactly its documented flights
                }

                var baseFare = baseFareForDuration(pair.DurationMinutes);
                var firstFlightNumber = GeneratedFlightNumberBase + (pairIndex * NumbersPerPair) + directionOffset;

                fullSchedule.Add(new ProviderScheduleMapper.ScheduledFlight(
                    $"{flightNumberPrefix}{firstFlightNumber}",
                    origin,
                    destination,
                    firstDepartureTimeOfDay,
                    pair.DurationMinutes,
                    baseFare));
                fullSchedule.Add(new ProviderScheduleMapper.ScheduledFlight(
                    $"{flightNumberPrefix}{firstFlightNumber + 1}",
                    origin,
                    destination,
                    secondDepartureTimeOfDay,
                    pair.DurationMinutes,
                    baseFare));
            }
        }

        return fullSchedule;
    }

    /// <summary>Ordinal route key ("LHR>JFK"). Fixture origins/destinations are uppercase by
    /// convention (SearchRequestValidator's uppercase-only regex), so ordinal is exact here;
    /// request-time case-insensitivity is ProviderScheduleMapper's concern, not this one's.</summary>
    private static string RouteKey(string origin, string destination) => $"{origin}>{destination}";
}
