namespace SkyRoute.Application.Validation;

/// <summary>
/// Single source of the "departure not in the past" calendar boundary, shared by
/// <see cref="SearchRequestValidator"/> (search departure date) and
/// <see cref="BookingRequestValidator"/> (booking flight-snapshot departure) so both endpoints
/// treat the day boundary identically (AUD-026/029/031).
///
/// The boundary is anchored one calendar day BEHIND UTC. Real-world civil time zones span
/// roughly UTC-12..UTC+14, so a client's legitimate LOCAL "today" can already read as
/// "yesterday" in UTC (a negative-offset user in the evening) or "tomorrow" in UTC. Comparing a
/// user-picked date against raw <c>DateOnly.FromDateTime(DateTime.UtcNow)</c> therefore wrongly
/// rejects a valid same-day local search once UTC has rolled to the next day (AUD-031). Anchoring
/// the boundary at UTC-1 accepts every timezone's legitimate "today" while still rejecting a date
/// that is unambiguously in the past for every timezone on Earth.
/// </summary>
internal static class DepartureDateRules
{
    /// <summary>
    /// The earliest calendar date accepted as "not in the past" — UTC today minus one day
    /// (timezone grace band). A date strictly less than this is rejected.
    /// </summary>
    public static DateOnly EarliestAcceptableDateUtc() =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
}
