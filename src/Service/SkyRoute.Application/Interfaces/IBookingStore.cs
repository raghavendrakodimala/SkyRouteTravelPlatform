using SkyRoute.Application.Domain;

namespace SkyRoute.Application.Interfaces;

/// <summary>
/// The sole persistence contract for bookings (DP-002, DP-PERSIST-001–005, BR-008).
/// Exposes only domain-object/scalar parameters and return types — no
/// ConcurrentDictionary&lt;,&gt;, IQueryable&lt;T&gt;, DbSet&lt;T&gt;, or any other
/// persistence-technology-specific type. Every operation carries a tenantId parameter
/// (DP-TENANT-006); the MVP in-memory implementation may ignore it for isolation purposes
/// but the parameter must remain present.
/// </summary>
public interface IBookingStore
{
    Task<Booking> CreateAsync(Booking booking, string tenantId, CancellationToken cancellationToken);

    Task<Booking?> GetByReferenceAsync(string reference, string tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string reference, string tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Booking>> ListByTenantAsync(string tenantId, int page, int pageSize, CancellationToken cancellationToken);
}
