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
}
