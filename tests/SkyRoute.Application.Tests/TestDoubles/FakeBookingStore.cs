using SkyRoute.Application.Domain;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Application.Tests.TestDoubles;

/// <summary>
/// Hand-written fake IBookingStore implementation, Dictionary-backed (no ConcurrentDictionary
/// needed for a single-threaded-test fake). CreateAsync mirrors InMemoryBookingStore's
/// TryAdd-based contract (code review finding CR-003): a real same-reference collision throws
/// DuplicateBookingReferenceException instead of silently overwriting. Also supports forcing a
/// collision on CreateAsync for the first N calls (or unconditionally), independent of whether
/// the reference is actually already stored, to deterministically exercise
/// BookingService's exception-driven retry loop (BR-004, Gap-fill BF-03) without needing to
/// predict BookingReferenceGenerator's real crypto-random output — this simulates the TOCTOU
/// race where a preceding ExistsAsync fast-path check passed but CreateAsync's atomic add
/// still collides.
/// </summary>
public sealed class FakeBookingStore : IBookingStore
{
    private readonly Dictionary<string, Booking> _bookings = new();

    /// <summary>Number of leading CreateAsync calls that should unconditionally collide.</summary>
    public int ForceCollisionForFirstNCalls { get; set; }

    /// <summary>When true, every CreateAsync call collides regardless of ForceCollisionForFirstNCalls.</summary>
    public bool AlwaysCollide { get; set; }

    public int ExistsAsyncCallCount { get; private set; }

    public int CreateAsyncCallCount { get; private set; }

    public Task<Booking> CreateAsync(Booking booking, string tenantId, CancellationToken cancellationToken)
    {
        CreateAsyncCallCount++;

        if (AlwaysCollide || CreateAsyncCallCount <= ForceCollisionForFirstNCalls)
        {
            throw new DuplicateBookingReferenceException(booking.BookingReference);
        }

        if (!_bookings.TryAdd(booking.BookingReference, booking))
        {
            throw new DuplicateBookingReferenceException(booking.BookingReference);
        }

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
