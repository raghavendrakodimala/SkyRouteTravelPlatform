using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Application.Tests.TestDoubles;

/// <summary>
/// Hand-written fake IBookingStore implementation, Dictionary-backed (no ConcurrentDictionary
/// needed for a single-threaded-test fake). Supports configuring ExistsAsync to return a fixed
/// value for the first N calls, then a different value thereafter, to exercise
/// BookingService's reference-collision retry loop (BR-004, Gap-fill BF-03) without needing to
/// predict BookingReferenceGenerator's real crypto-random output.
/// </summary>
public sealed class FakeBookingStore : IBookingStore
{
    private readonly Dictionary<string, Booking> _bookings = new();

    /// <summary>Number of leading ExistsAsync calls that should unconditionally return true.</summary>
    public int ForceCollisionForFirstNCalls { get; set; }

    /// <summary>When true, every ExistsAsync call returns true regardless of ForceCollisionForFirstNCalls.</summary>
    public bool AlwaysCollide { get; set; }

    public int ExistsAsyncCallCount { get; private set; }

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
        ExistsAsyncCallCount++;

        if (AlwaysCollide)
        {
            return Task.FromResult(true);
        }

        if (ExistsAsyncCallCount <= ForceCollisionForFirstNCalls)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(_bookings.ContainsKey(reference));
    }

    public Task<IReadOnlyList<Booking>> ListByTenantAsync(string tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyList<Booking> results = _bookings.Values
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.CreatedAtUtc)
            .Skip((Math.Max(1, page) - 1) * Math.Max(0, pageSize))
            .Take(Math.Max(0, pageSize))
            .ToList();

        return Task.FromResult(results);
    }
}
