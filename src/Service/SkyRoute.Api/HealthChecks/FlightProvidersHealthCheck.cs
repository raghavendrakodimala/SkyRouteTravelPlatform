using Microsoft.Extensions.Diagnostics.HealthChecks;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Api.HealthChecks;

/// <summary>
/// Reports the health of the flight-provider registrations (PO directive 2026-07-08): the
/// aggregator needs at least one provider to return results; the MVP ships with two
/// (GlobalAir, BudgetWings — FR-053). Fewer than two registered is Degraded (fault-isolated
/// aggregation still works, coverage is reduced); zero is Unhealthy.
/// </summary>
public sealed class FlightProvidersHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IFlightProvider> _providers;

    public FlightProvidersHealthCheck(IEnumerable<IFlightProvider> providers)
    {
        _providers = providers;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var names = _providers.Select(p => p.GetType().Name).ToArray();
        var description = $"{names.Length} provider(s) registered: {string.Join(", ", names)}";

        var result = names.Length switch
        {
            0 => HealthCheckResult.Unhealthy("No flight providers registered."),
            1 => HealthCheckResult.Degraded(description),
            _ => HealthCheckResult.Healthy(description),
        };

        return Task.FromResult(result);
    }
}
