namespace SkyRoute.Application.Interfaces;

/// <summary>
/// The multi-tenancy seam (DP-TENANT-001). A single read-only property resolved by the
/// infrastructure layer; MVP's DefaultTenantContext always returns "default".
/// </summary>
public interface ITenantContext
{
    string TenantId { get; }
}
