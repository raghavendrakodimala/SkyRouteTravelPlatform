using SkyRoute.Application.Contracts;
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
    /// SEC-001 (Phase 16 security review, BR-006, NFR-DATA-002): authoritative server-side
    /// fare re-resolution, independent of anything a client submits on POST /api/bookings.
    /// Re-invokes this provider's own pricing rule (BR-001/BR-002) for a known flight number
    /// and cabin class, the same combination <see cref="SearchAsync"/> already prices by.
    /// Returns <c>false</c> (and zeroes both out-values) if <paramref name="flightNumber"/> is
    /// not part of this provider's fixed schedule — a caller cannot use an unknown flight
    /// number to bypass fare verification.
    /// </summary>
    bool TryResolveFare(string flightNumber, string cabinClass, out decimal baseFare, out decimal pricePerPassenger);
}
