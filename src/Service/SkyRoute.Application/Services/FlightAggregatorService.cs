using Microsoft.Extensions.Logging;
using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Application.Services;

/// <summary>
/// Aggregation orchestration (DP-004, BR-007, FR-049, FR-050). Invokes all registered
/// IFlightProvider instances concurrently. AD-010: fault isolation is delivered by wrapping
/// each provider invocation in its own try/catch (SafeInvokeAsync) BEFORE the task is handed
/// to Task.WhenAll — Task.WhenAll alone does not swallow an individual task's exception.
/// </summary>
public sealed class FlightAggregatorService : IFlightAggregatorService
{
    private readonly IEnumerable<IFlightProvider> _providers;
    private readonly ILogger<FlightAggregatorService> _logger;

    public FlightAggregatorService(IEnumerable<IFlightProvider> providers, ILogger<FlightAggregatorService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<FlightSearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var providers = _providers.ToList();
        var outcomes = await Task.WhenAll(providers.Select(provider => SafeInvokeAsync(provider, request, cancellationToken)));

        var flights = outcomes.SelectMany(outcome => outcome.Results).ToList();

        // AUD-038: total outage = at least one provider was registered AND every one of them
        // failed. Zero providers registered is a configuration concern surfaced by the health
        // check, not a runtime outage, so it is NOT treated as a total failure (returns 200 []).
        // Any single success keeps fault isolation intact and yields a normal 200 response.
        var allProvidersFailed = providers.Count > 0 && outcomes.All(outcome => outcome.Failed);

        return new FlightSearchResult(flights, allProvidersFailed);
    }

    private async Task<(IReadOnlyList<FlightResult> Results, bool Failed)> SafeInvokeAsync(
        IFlightProvider provider, SearchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return (await provider.SearchAsync(request, cancellationToken), Failed: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {ProviderName} failed during search", provider.ProviderName);
            return ([], Failed: true);
        }
    }
}
