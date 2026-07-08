using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Persistence;

/// <summary>
/// EF Core (SQLite) implementation of <see cref="IBookingStore"/> (DP-PERSIST-001–005, BR-008).
/// Coexists with <see cref="InMemoryBookingStore"/> behind the same contract — swapping between
/// them is a single DI line (see PersistenceServiceCollectionExtensions), demonstrating that no
/// persistence-technology type leaks through <see cref="IBookingStore"/>.
///
/// Uniqueness (NFR-DATA-001) is enforced by the Bookings PK, not by a check-then-act pre-read:
/// a duplicate BookingReference makes SaveChanges throw a SQLite UNIQUE/PRIMARY KEY violation,
/// which this store translates into the SAME <see cref="DuplicateBookingReferenceException"/> the
/// in-memory store throws — so BookingService's CR-003 bounded-retry loop works unchanged.
/// </summary>
public sealed class EfCoreBookingStore : IBookingStore
{
    // SQLITE_CONSTRAINT = 19; extended codes for the two "reference already exists" cases.
    private const int SqliteConstraint = 19;
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintUnique = 2067;

    private readonly AppDbContext _db;

    public EfCoreBookingStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Booking> CreateAsync(Booking booking, string tenantId, CancellationToken cancellationToken)
    {
        var entry = _db.Bookings.Add(booking);

        // Materialised, DB-CHECK-guarded shadow column (§5.4 item 3) — derived from the list, kept
        // off the domain object. Set post-Add: it is a non-key column on the already-tracked root.
        entry.Property("PassengerCount").CurrentValue = booking.Passengers.Count;

        // Assign the order-preserving 1-based ordinal (shadow PK part) from the request list index.
        // Add() gave each passenger a distinct temp key, so these post-Add assignments do not collide.
        // 1-based (not 0-based) because a value-generated key treats 0 (the CLR default) as "unset,
        // generate it" and would send NULL. EF materialises the collection ordered by this key, so
        // read order == request order.
        for (var i = 0; i < booking.Passengers.Count; i++)
        {
            _db.Entry(booking.Passengers[i]).Property("PassengerOrdinal").CurrentValue = i + 1;
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateReferenceViolation(ex))
        {
            // Detach the failed graph so BookingService's retry (same scoped DbContext) starts
            // clean and does not re-attempt this rejected insert on the next SaveChanges. Only this
            // booking is tracked on the create path (ExistsAsync/queries are AsNoTracking).
            _db.ChangeTracker.Clear();
            throw new DuplicateBookingReferenceException(booking.BookingReference);
        }

        // Return the same instance the caller passed (matching InMemoryBookingStore) so the response
        // reflects exactly the submitted values; the DB copy is what GetByReference/List read back.
        return booking;
    }

    public async Task<Booking?> GetByReferenceAsync(string reference, string tenantId, CancellationToken cancellationToken)
    {
        return await _db.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.BookingReference == reference && b.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(string reference, string tenantId, CancellationToken cancellationToken)
    {
        return await _db.Bookings
            .AsNoTracking()
            .AnyAsync(b => b.BookingReference == reference && b.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> ListByTenantAsync(string tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Max(0, pageSize);

        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// True only for a duplicate PK/UNIQUE violation (reference already exists). A CHECK violation
    /// (SQLITE_CONSTRAINT_CHECK, extended 275) is deliberately NOT matched — that means bad data
    /// slipped past application validation and must surface as a real 500, not a duplicate-retry.
    /// </summary>
    private static bool IsDuplicateReferenceViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException sqlite
        && sqlite.SqliteErrorCode == SqliteConstraint
        && sqlite.SqliteExtendedErrorCode is SqliteConstraintPrimaryKey or SqliteConstraintUnique;
}
