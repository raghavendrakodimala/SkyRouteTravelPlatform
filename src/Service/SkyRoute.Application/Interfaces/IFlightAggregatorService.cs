using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;

namespace SkyRoute.Application.Interfaces;

/// <summary>
/// The contract between the search API controller and aggregation orchestration logic
/// (DP-004, FR-046). The controller must not call IFlightProvider implementations directly.
/// </summary>
public interface IFlightAggregatorService
{
    Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}
