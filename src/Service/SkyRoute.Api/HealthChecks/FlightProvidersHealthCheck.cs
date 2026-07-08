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
        // AUD-035: build the description from the stable, public ProviderName rather than the CLR
        // GetType().Name, and count only — do not enumerate the internal composition into an
        // always-on unauthenticated surface. The public /api/health writer omits descriptions
        // entirely (Program.cs); this text is retained for server-side logging only.
        var count = _providers.Count();

        var result = count switch
        {
            0 => HealthCheckResult.Unhealthy("No flight providers registered."),
            1 => HealthCheckResult.Degraded("1 flight provider registered."),
            _ => HealthCheckResult.Healthy($"{count} flight providers registered."),
        };

        return Task.FromResult(result);
    }
}
