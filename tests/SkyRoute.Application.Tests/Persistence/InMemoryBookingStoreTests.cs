using SkyRoute.Application.Domain;
using SkyRoute.Infrastructure.Persistence;

namespace SkyRoute.Application.Tests.Persistence;

/// <summary>
/// Unit tests for InMemoryBookingStore (BR-008, NFR-SCALE-002). The SUT lives in
/// SkyRoute.Infrastructure; this test lives in the Application test project's Persistence
/// folder for simplicity, since SkyRoute.Application.Tests already references
/// SkyRoute.Infrastructure — a third test project was not warranted for a single class.
/// </summary>
public class InMemoryBookingStoreTests
{
    private static Booking MakeBooking(
        string reference = "SKY-INT-ABC123",
        string tenantId = "default",
        DateTime? createdAtUtc = null) => new()
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
        Passengers = new List<PassengerDetail>
        {
            new()
            {
                FullName = "Jane Doe",
                Email = "jane@example.com",
                DocumentType = "Passport",
                DocumentNumber = "AB1234C",
            },
        },
        CabinClass = "Economy",
        TotalPrice = 287.50m,
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
        TenantId = tenantId,
    };

    [Fact]
    public async Task ExistsAsync_AfterCreate_ReturnsTrueForSameReference()
    {
        var store = new InMemoryBookingStore();
        var booking = MakeBooking();
        await store.CreateAsync(booking, "default", CancellationToken.None);

        var exists = await store.ExistsAsync(booking.BookingReference, "default", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ForReferenceNeverCreated_ReturnsFalse()
    {
        var store = new InMemoryBookingStore();

        var exists = await store.ExistsAsync("SKY-INT-ZZZZZZ", "default", CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task GetByReferenceAsync_AfterCreate_ReturnsTheSameBooking()
    {
        var store = new InMemoryBookingStore();
        var booking = MakeBooking();
        await store.CreateAsync(booking, "default", CancellationToken.None);

        var retrieved = await store.GetByReferenceAsync(booking.BookingReference, "default", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(booking.BookingReference, retrieved!.BookingReference);
        Assert.Equal(booking.TotalPrice, retrieved.TotalPrice);
    }

    [Fact]
    public async Task GetByReferenceAsync_ForUnknownReference_ReturnsNull()
    {
        var store = new InMemoryBookingStore();

        var retrieved = await store.GetByReferenceAsync("SKY-INT-ZZZZZZ", "default", CancellationToken.None);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task CreateAsync_ConcurrentWrites_AllFiftyBookingsAreRetrievable()
    {
        // BR-008/NFR-SCALE-002 thread-safety smoke test: distinct references created
        // concurrently must not be lost by the ConcurrentDictionary-backed implementation.
        var store = new InMemoryBookingStore();
        var tasks = Enumerable.Range(0, 50)
            .Select(i => store.CreateAsync(MakeBooking(reference: $"SKY-INT-C{i:D5}"), "default", CancellationToken.None));

        await Task.WhenAll(tasks);

        var existsChecks = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(i => store.ExistsAsync($"SKY-INT-C{i:D5}", "default", CancellationToken.None)));
        Assert.All(existsChecks, Assert.True);

        var retrieved = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(i => store.GetByReferenceAsync($"SKY-INT-C{i:D5}", "default", CancellationToken.None)));
        Assert.All(retrieved, b => Assert.NotNull(b));
    }

    [Fact]
    public async Task ListByTenantAsync_FiltersByTenantId()
    {
        var store = new InMemoryBookingStore();
        await store.CreateAsync(MakeBooking(reference: "SKY-INT-AAA111", tenantId: "tenantA"), "tenantA", CancellationToken.None);
        await store.CreateAsync(MakeBooking(reference: "SKY-INT-BBB222", tenantId: "tenantB"), "tenantB", CancellationToken.None);

        var tenantAResults = await store.ListByTenantAsync("tenantA", 1, 10, CancellationToken.None);

        Assert.Single(tenantAResults);
        Assert.Equal("SKY-INT-AAA111", tenantAResults[0].BookingReference);
    }

    [Fact]
    public async Task ListByTenantAsync_Paging_BehavesAsSkipTake()
    {
        var store = new InMemoryBookingStore();
        for (var i = 0; i < 5; i++)
        {
            await store.CreateAsync(
                MakeBooking(reference: $"SKY-INT-P{i:D5}", tenantId: "tenantA", createdAtUtc: DateTime.UtcNow.AddSeconds(i)),
                "tenantA",
                CancellationToken.None);
        }

        var page1 = await store.ListByTenantAsync("tenantA", 1, 2, CancellationToken.None);
        var page2 = await store.ListByTenantAsync("tenantA", 2, 2, CancellationToken.None);
        var page3 = await store.ListByTenantAsync("tenantA", 3, 2, CancellationToken.None);

        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.Single(page3);

        Assert.Equal("SKY-INT-P00000", page1[0].BookingReference);
        Assert.Equal("SKY-INT-P00001", page1[1].BookingReference);
        Assert.Equal("SKY-INT-P00002", page2[0].BookingReference);
        Assert.Equal("SKY-INT-P00003", page2[1].BookingReference);
        Assert.Equal("SKY-INT-P00004", page3[0].BookingReference);
    }
}
