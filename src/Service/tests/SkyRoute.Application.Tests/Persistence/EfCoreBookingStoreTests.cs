using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Exceptions;
using SkyRoute.Infrastructure.Persistence;

namespace SkyRoute.Application.Tests.Persistence;

/// <summary>
/// Unit tests for <see cref="EfCoreBookingStore"/> over a real (SQLite in-memory) database
/// (DATA-MODEL-001, DP-PERSIST-001–005). Each test owns an isolated, schema-created in-memory
/// database backed by a single kept-open connection — the exact pattern Program.cs uses — and
/// disposes it at the end. These tests prove (1) the immutable domain round-trips through EF, with
/// the flight snapshot and passenger order preserved; (2) a duplicate reference is translated to
/// the same <see cref="DuplicateBookingReferenceException"/> the in-memory store throws, so
/// BookingService's retry loop is unaffected; (3) tenant listing is filtered and ordered; and
/// (4) the DB CHECK constraints actually reject bad data rather than trusting the caller.
/// </summary>
public sealed class EfCoreBookingStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public EfCoreBookingStoreTests()
    {
        // One connection, opened once and kept open — a :memory: DB lives only while its connection
        // is open, so every AppDbContext in a test shares this one connection = one shared schema.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext NewContext() => new(_options);

    private static Booking MakeBooking(
        string reference = "SKY-INT-ABC123",
        string tenantId = "default",
        DateTime? createdAtUtc = null,
        IReadOnlyList<PassengerDetail>? passengers = null) => new()
    {
        BookingReference = reference,
        Flight = new BookingFlightSnapshot
        {
            Provider = "GlobalAir",
            FlightNumber = "GA101",
            Origin = "LHR",
            Destination = "JFK",
            DepartureDateTime = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
            ArrivalDateTime = new DateTime(2026, 8, 1, 17, 30, 0, DateTimeKind.Utc),
            CabinClass = "Economy",
            PricePerPassenger = 287.50m,
        },
        Passengers = passengers ?? new List<PassengerDetail>
        {
            new()
            {
                FullName = "Jane Doe",
                Age = 34,
                Email = "jane@example.com",
                DocumentType = "Passport",
                DocumentNumber = "AB1234C",
            },
        },
        CabinClass = "Economy",
        TotalPrice = 287.50m,
        CreatedAtUtc = createdAtUtc ?? new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc),
        TenantId = tenantId,
    };

    private static PassengerDetail MakePassenger(string fullName, int age = 34, string documentNumber = "AB1234C") => new()
    {
        FullName = fullName,
        Age = age,
        Email = $"{fullName.Replace(" ", ".").ToLowerInvariant()}@example.com",
        DocumentType = "Passport",
        DocumentNumber = documentNumber,
    };

    [Fact]
    public async Task CreateThenGet_RoundTripsTheImmutableAggregateThroughTheDatabase()
    {
        var booking = MakeBooking(passengers: new List<PassengerDetail>
        {
            MakePassenger("Jane Doe", age: 34, documentNumber: "AB1234C"),
            MakePassenger("John Smith", age: 8, documentNumber: "CD5678E"),
        });

        // Write through one context/store...
        await using (var writeContext = NewContext())
        {
            var store = new EfCoreBookingStore(writeContext);
            await store.CreateAsync(booking, "default", CancellationToken.None);
        }

        // ...and read back through a completely fresh context so this genuinely materialises from
        // the DB (not from the change tracker), proving EF can rebuild the required/init domain.
        await using var readContext = NewContext();
        var readStore = new EfCoreBookingStore(readContext);
        var retrieved = await readStore.GetByReferenceAsync("SKY-INT-ABC123", "default", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal("SKY-INT-ABC123", retrieved!.BookingReference);
        Assert.Equal(287.50m, retrieved.TotalPrice);
        Assert.Equal("Economy", retrieved.CabinClass);
        Assert.Equal("default", retrieved.TenantId);

        // Owned single flight snapshot round-tripped onto the Bookings columns.
        Assert.Equal("GlobalAir", retrieved.Flight.Provider);
        Assert.Equal("GA101", retrieved.Flight.FlightNumber);
        Assert.Equal("LHR", retrieved.Flight.Origin);
        Assert.Equal("JFK", retrieved.Flight.Destination);
        Assert.Equal(287.50m, retrieved.Flight.PricePerPassenger);
        Assert.True(retrieved.Flight.ArrivalDateTime > retrieved.Flight.DepartureDateTime);

        // Owned collection round-tripped into BookingPassengers, in the original request order.
        Assert.Equal(2, retrieved.Passengers.Count);
        Assert.Equal(new[] { "Jane Doe", "John Smith" }, retrieved.Passengers.Select(p => p.FullName).ToArray());
        Assert.Equal(new[] { 34, 8 }, retrieved.Passengers.Select(p => p.Age).ToArray());
        Assert.Equal("AB1234C", retrieved.Passengers[0].DocumentNumber);
        Assert.Equal("CD5678E", retrieved.Passengers[1].DocumentNumber);
    }

    [Fact]
    public async Task ExistsAsync_ReflectsDatabaseState()
    {
        await using var context = NewContext();
        var store = new EfCoreBookingStore(context);

        Assert.False(await store.ExistsAsync("SKY-INT-ABC123", "default", CancellationToken.None));

        await store.CreateAsync(MakeBooking(), "default", CancellationToken.None);

        Assert.True(await store.ExistsAsync("SKY-INT-ABC123", "default", CancellationToken.None));
        Assert.False(await store.ExistsAsync("SKY-INT-ZZZZZZ", "default", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_DuplicateReference_ThrowsDuplicateBookingReferenceException()
    {
        // Seed the reference via one context (committed to the shared DB).
        await using (var seedContext = NewContext())
        {
            var seedStore = new EfCoreBookingStore(seedContext);
            await seedStore.CreateAsync(MakeBooking(reference: "SKY-INT-DUP001"), "default", CancellationToken.None);
        }

        // A separate context (does not track the seeded row) hits the real PK violation on save —
        // it must surface as the SAME exception the in-memory store throws, so BookingService's
        // CR-003 retry loop keeps working unchanged.
        await using var collidingContext = NewContext();
        var collidingStore = new EfCoreBookingStore(collidingContext);

        await Assert.ThrowsAsync<DuplicateBookingReferenceException>(() =>
            collidingStore.CreateAsync(MakeBooking(reference: "SKY-INT-DUP001"), "default", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_AfterDuplicateRejection_SameContextCanRetryWithNewReference()
    {
        // Seed a reference so the first attempt below collides at the DB.
        await using (var seedContext = NewContext())
        {
            var seedStore = new EfCoreBookingStore(seedContext);
            await seedStore.CreateAsync(MakeBooking(reference: "SKY-INT-RTRY01"), "default", CancellationToken.None);
        }

        // Mirrors the BookingService retry loop: one scoped context, collide then retry. The store
        // must detach the failed graph on the duplicate so the retry's SaveChanges is clean.
        await using var context = NewContext();
        var store = new EfCoreBookingStore(context);

        await Assert.ThrowsAsync<DuplicateBookingReferenceException>(() =>
            store.CreateAsync(MakeBooking(reference: "SKY-INT-RTRY01"), "default", CancellationToken.None));

        var retried = await store.CreateAsync(MakeBooking(reference: "SKY-INT-RTRY02"), "default", CancellationToken.None);
        Assert.Equal("SKY-INT-RTRY02", retried.BookingReference);

        await using var verifyContext = NewContext();
        var verifyStore = new EfCoreBookingStore(verifyContext);
        Assert.True(await verifyStore.ExistsAsync("SKY-INT-RTRY02", "default", CancellationToken.None));
    }

    [Fact]
    public async Task ListByTenantAsync_FiltersByTenantAndOrdersByCreatedAtUtc()
    {
        await using var context = NewContext();
        var store = new EfCoreBookingStore(context);

        // Insert out of chronological order and across tenants.
        await store.CreateAsync(
            MakeBooking(reference: "SKY-INT-T00003", tenantId: "tenantA", createdAtUtc: new DateTime(2026, 7, 8, 12, 0, 3, DateTimeKind.Utc)),
            "tenantA", CancellationToken.None);
        await store.CreateAsync(
            MakeBooking(reference: "SKY-INT-T00001", tenantId: "tenantA", createdAtUtc: new DateTime(2026, 7, 8, 12, 0, 1, DateTimeKind.Utc)),
            "tenantA", CancellationToken.None);
        await store.CreateAsync(
            MakeBooking(reference: "SKY-INT-T00002", tenantId: "tenantA", createdAtUtc: new DateTime(2026, 7, 8, 12, 0, 2, DateTimeKind.Utc)),
            "tenantA", CancellationToken.None);
        await store.CreateAsync(
            MakeBooking(reference: "SKY-INT-OTHER1", tenantId: "tenantB", createdAtUtc: new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc)),
            "tenantB", CancellationToken.None);

        var tenantA = await store.ListByTenantAsync("tenantA", 1, 10, CancellationToken.None);

        Assert.Equal(3, tenantA.Count);
        Assert.Equal(
            new[] { "SKY-INT-T00001", "SKY-INT-T00002", "SKY-INT-T00003" },
            tenantA.Select(b => b.BookingReference).ToArray());
        Assert.DoesNotContain(tenantA, b => b.TenantId == "tenantB");
    }

    [Fact]
    public async Task ListByTenantAsync_Paging_BehavesAsSkipTake()
    {
        await using var context = NewContext();
        var store = new EfCoreBookingStore(context);
        for (var i = 0; i < 5; i++)
        {
            await store.CreateAsync(
                MakeBooking(reference: $"SKY-INT-P{i:D5}", tenantId: "tenantA", createdAtUtc: new DateTime(2026, 7, 8, 12, 0, i, DateTimeKind.Utc)),
                "tenantA", CancellationToken.None);
        }

        var page1 = await store.ListByTenantAsync("tenantA", 1, 2, CancellationToken.None);
        var page2 = await store.ListByTenantAsync("tenantA", 2, 2, CancellationToken.None);
        var page3 = await store.ListByTenantAsync("tenantA", 3, 2, CancellationToken.None);

        Assert.Equal(new[] { "SKY-INT-P00000", "SKY-INT-P00001" }, page1.Select(b => b.BookingReference).ToArray());
        Assert.Equal(new[] { "SKY-INT-P00002", "SKY-INT-P00003" }, page2.Select(b => b.BookingReference).ToArray());
        Assert.Equal(new[] { "SKY-INT-P00004" }, page3.Select(b => b.BookingReference).ToArray());
    }

    [Fact]
    public async Task CreateAsync_PassengerCountBelowOne_RejectedByDatabaseCheckConstraint()
    {
        // Zero passengers materialises PassengerCount = 0, violating CK_Bookings_PaxCount (1..9).
        // It must surface as a raw DbUpdateException (a real integrity failure), NOT be
        // mis-translated into DuplicateBookingReferenceException.
        await using var context = NewContext();
        var store = new EfCoreBookingStore(context);
        var booking = MakeBooking(reference: "SKY-INT-NOPAX0", passengers: new List<PassengerDetail>());

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            store.CreateAsync(booking, "default", CancellationToken.None));
        Assert.IsNotType<DuplicateBookingReferenceException>(ex);
    }

    [Fact]
    public async Task CreateAsync_AgeAboveMaximum_RejectedByDatabaseCheckConstraint()
    {
        // Age 200 violates CK_BookingPassengers_Age (0..120) — proving the constraint is enforced
        // at the DB boundary, not merely by application validation.
        await using var context = NewContext();
        var store = new EfCoreBookingStore(context);
        var booking = MakeBooking(
            reference: "SKY-INT-OLDAGE",
            passengers: new List<PassengerDetail> { MakePassenger("Old Person", age: 200, documentNumber: "AB1234C") });

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            store.CreateAsync(booking, "default", CancellationToken.None));
    }
}
