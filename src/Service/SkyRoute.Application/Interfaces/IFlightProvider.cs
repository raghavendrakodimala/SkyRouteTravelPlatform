using SkyRoute.Application.Dtos;
using SkyRoute.Application.Domain;

namespace SkyRoute.Application.Interfaces;

/// <summary>
/// The sole integration contract between the aggregation layer and any provider
/// implementation (DP-001, FR-046, FR-047). No code outside a provider class may
/// reference a concrete provider type.
/// </summary>
public interface IFlightProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// SEC-001 (Phase 16 security review, BR-006, NFR-DATA-002) and AUD-025/028/033:
    /// authoritative server-side re-resolution of a flight's identifying snapshot, independent
    /// of anything a client submits on POST /api/bookings. From a single schedule lookup keyed
    /// on <paramref name="flightNumber"/>/<paramref name="cabinClass"/> it surfaces BOTH the
    /// re-derived fare (this provider's own BR-001/BR-002 pricing rule, the same combination
    /// <see cref="SearchAsync"/> prices by) AND the authoritative <paramref name="origin"/>/
    /// <paramref name="destination"/> for that flight number — so the booking service can reject
    /// a forged route and derive the BR-003 document rule from the real route, not client fields
    /// (one lookup, no duplicated schedule scan). Returns <c>false</c> (and zeroes/nulls all
    /// out-values) if <paramref name="flightNumber"/> is not part of this provider's fixed
    /// schedule — a caller cannot use an unknown flight number to bypass verification.
    /// </summary>
    bool TryResolveFare(
        string flightNumber,
        string cabinClass,
        out decimal baseFare,
        out decimal pricePerPassenger,
        out string? origin,
        out string? destination);
}
