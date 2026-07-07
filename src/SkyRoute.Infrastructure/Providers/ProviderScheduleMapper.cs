using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Shared schedule-to-<see cref="FlightResult"/> mapping pipeline used by every mock flight
/// provider (CR-002 fix). Each provider owns its own fixed <see cref="ScheduledFlight"/>
/// schedule and its own named, private static pricing method (kept distinct per provider —
/// DP-006/DP-019 per-provider testability intent is preserved); only the departure-date
/// fallback, cabin multiplier lookup, base-fare rounding, UTC timestamp construction,
/// arrival-time computation, and <see cref="FlightResult"/> object-initializer are shared
/// here to avoid duplicating that ~25-line pipeline in every provider class
/// (architecture-plan.md Section 3.8 extensibility goal, NFR-MAINT-002/DRY).
/// </summary>
internal static class ProviderScheduleMapper
{
    /// <summary>
    /// A single fixed schedule entry shared by all mock providers' hardcoded datasets.
    /// </summary>
    internal sealed record ScheduledFlight(
        string FlightNumber,
        string Origin,
        string Destination,
        TimeOnly DepartureTimeOfDay,
        int DurationMinutes,
        decimal EconomyBaseFare);

    /// <summary>
    /// Maps a provider's fixed <paramref name="schedule"/> to <see cref="FlightResult"/>s for
    /// the given <paramref name="request"/>, applying <paramref name="providerName"/> and the
    /// caller-supplied <paramref name="applyPricing"/> delegate (the provider's own named
    /// pricing method, e.g. ApplyGlobalAirPricing/ApplyBudgetWingsPricing). No
    /// behavior/pricing/schedule change relative to the previous per-provider copies.
    /// </summary>
    public static IReadOnlyList<FlightResult> BuildResults(
        IReadOnlyList<ScheduledFlight> schedule,
        SearchRequest request,
        string providerName,
        Func<decimal, decimal> applyPricing)
    {
        var departureDate = request.DepartureDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var cabinMultiplier = CabinClassMultipliers.ForCabinClass(request.CabinClass);

        return schedule.Select(flight =>
        {
            var baseFare = Math.Round(flight.EconomyBaseFare * cabinMultiplier, 2, MidpointRounding.AwayFromZero);
            var departureDateTime = DateTime.SpecifyKind(departureDate.ToDateTime(flight.DepartureTimeOfDay), DateTimeKind.Utc);
            var arrivalDateTime = departureDateTime.AddMinutes(flight.DurationMinutes);

            return new FlightResult
            {
                Provider = providerName,
                FlightNumber = flight.FlightNumber,
                Origin = flight.Origin,
                Destination = flight.Destination,
                DepartureDateTime = departureDateTime,
                ArrivalDateTime = arrivalDateTime,
                DurationMinutes = flight.DurationMinutes,
                CabinClass = request.CabinClass,
                BaseFare = baseFare,
                PricePerPassenger = applyPricing(baseFare),
            };
        }).ToList();
    }
}
