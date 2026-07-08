using SkyRoute.Application.Dtos;
using SkyRoute.Application.Domain;

namespace SkyRoute.Application.Interfaces;

/// <summary>
/// The contract between the search API controller and aggregation orchestration logic
/// (DP-004, FR-046). The controller must not call IFlightProvider implementations directly.
/// </summary>
public interface IFlightAggregatorService
{
    /// <summary>
    /// Runs every registered provider with per-provider fault isolation and returns the merged
    /// results plus a total-outage signal (AUD-038). See <see cref="FlightSearchResult"/> for
    /// how the controller maps a total outage (503) apart from a genuine no-match (200 []).
    /// </summary>
    Task<FlightSearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}
