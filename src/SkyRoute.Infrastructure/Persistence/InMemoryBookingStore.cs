using System.Collections.Concurrent;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Persistence;

/// <summary>
/// Thread-safe, singleton, in-memory IBookingStore implementation (BR-008). Backed by a
/// ConcurrentDictionary keyed by booking reference — that type never appears in the
/// IBookingStore interface itself (DP-PERSIST-001). The tenantId parameter is accepted on
/// every operation (DP-TENANT-006) but not enforced for isolation in this MVP single-tenant
/// implementation (ListByTenantAsync filters by it for correctness of that specific query).
/// </summary>
public sealed class InMemoryBookingStore : IBookingStore
{
    private readonly ConcurrentDictionary<string, Booking> _bookings = new();

    public Task<Booking> CreateAsync(Booking booking, string tenantId, CancellationToken cancellationToken)
    {
        _bookings[booking.BookingReference] = booking;
        return Task.FromResult(booking);
    }

    public Task<Booking?> GetByReferenceAsync(string reference, string tenantId, CancellationToken cancellationToken)
    {
        _bookings.TryGetValue(reference, out var booking);
        return Task.FromResult(booking);
    }

    public Task<bool> ExistsAsync(string reference, string tenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_bookings.ContainsKey(reference));
    }

    public Task<IReadOnlyList<Booking>> ListByTenantAsync(string tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Max(0, pageSize);

        IReadOnlyList<Booking> results = _bookings.Values
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return Task.FromResult(results);
    }
}
