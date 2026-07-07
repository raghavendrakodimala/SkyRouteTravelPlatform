using Microsoft.Extensions.Diagnostics.HealthChecks;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Api.HealthChecks;

/// <summary>
/// Reports the health of the booking persistence layer (PO directive 2026-07-08): performs a
/// real, cheap read roundtrip against <see cref="IBookingStore"/> rather than merely checking
/// DI resolvability — when a real database replaces the in-memory store (NFR-ONPREM-003),
/// this same check exercises actual connectivity with no changes here.
/// </summary>
public sealed class BookingStoreHealthCheck : IHealthCheck
{
    private readonly IBookingStore _store;
    private readonly ITenantContext _tenantContext;

    public BookingStoreHealthCheck(IBookingStore store, ITenantContext tenantContext)
    {
        _store = store;
        _tenantContext = tenantContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bookings = await _store.ListByTenantAsync(_tenantContext.TenantId, page: 1, pageSize: 1, cancellationToken);
            return HealthCheckResult.Healthy(
                $"Booking store is reachable ({(bookings.Count > 0 ? "has bookings" : "empty")}).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Booking store roundtrip failed.", ex);
        }
    }
}
