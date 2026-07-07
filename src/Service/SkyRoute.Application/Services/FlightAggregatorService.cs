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

    public async Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var tasks = _providers.Select(provider => SafeInvokeAsync(provider, request, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private async Task<IReadOnlyList<FlightResult>> SafeInvokeAsync(
        IFlightProvider provider, SearchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await provider.SearchAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {ProviderName} failed during search", provider.ProviderName);
            return Array.Empty<FlightResult>();
        }
    }
}
