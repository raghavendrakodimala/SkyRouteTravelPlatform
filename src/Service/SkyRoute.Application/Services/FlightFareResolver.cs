using SkyRoute.Application.Interfaces;

namespace SkyRoute.Application.Services;

/// <summary>
/// Authoritative server-side flight fare re-resolution (SEC-001, Phase 16 security review;
/// BR-006, NFR-DATA-002). Mirrors <see cref="RouteTypeResolver"/>'s established pattern
/// (BR-003/DP-016) of re-deriving a server-owned fact from the flight snapshot's identifying
/// fields at booking time rather than trusting the client-submitted value. Re-invokes the
/// same provider pricing logic the search endpoint already uses
/// (<c>GlobalAirProvider.ApplyGlobalAirPricing</c> / <c>BudgetWingsProvider.ApplyBudgetWingsPricing</c>,
/// via each provider's own <see cref="IFlightProvider.TryResolveFare"/>), keyed by
/// Provider + FlightNumber + CabinClass — the same fields the providers' fixed mock
/// schedules and BR-001/BR-002 pricing rules depend on. Pricing is fully deterministic given
/// these three fields (departure date only affects the calendar date of the returned
/// timestamps, never the price), so this resolver requires an exact match — no tolerance.
/// </summary>
public sealed class FlightFareResolver
{
    private readonly IEnumerable<IFlightProvider> _providers;

    public FlightFareResolver(IEnumerable<IFlightProvider> providers)
    {
        _providers = providers;
    }

    /// <summary>
    /// Resolves <paramref name="providerName"/> against the registered <see cref="IFlightProvider"/>
    /// instances (ordinal match, consistent with FlightResult.Provider being a fixed,
    /// server-defined string rather than user free text) and, if found, re-derives the fare
    /// for <paramref name="flightNumber"/>/<paramref name="cabinClass"/>. Returns
    /// <c>false</c> (zeroed out-values) if the provider name is unknown or the flight number
    /// is not part of that provider's schedule — either case means the fare cannot be
    /// authoritatively verified, and the caller must treat that as a validation failure
    /// rather than falling back to trusting the client-submitted value.
    /// </summary>
    public bool TryResolveFare(
        string? providerName, string? flightNumber, string? cabinClass, out decimal baseFare, out decimal pricePerPassenger)
    {
        baseFare = 0m;
        pricePerPassenger = 0m;

        if (string.IsNullOrWhiteSpace(providerName) ||
            string.IsNullOrWhiteSpace(flightNumber) ||
            string.IsNullOrWhiteSpace(cabinClass))
        {
            return false;
        }

        var provider = _providers.FirstOrDefault(p => string.Equals(p.ProviderName, providerName, StringComparison.Ordinal));
        return provider is not null && provider.TryResolveFare(flightNumber, cabinClass, out baseFare, out pricePerPassenger);
    }
}
