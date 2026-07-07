using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Tenancy;

/// <summary>
/// MVP's ITenantContext implementation (DP-TENANT-002) — always resolves to "default".
/// A future JwtTenantContext/HeaderTenantContext can be substituted with zero changes to
/// BookingService or any controller (DP-TENANT-003/004).
/// </summary>
public sealed class DefaultTenantContext : ITenantContext
{
    public string TenantId => "default";
}
