using System.Text.RegularExpressions;
using SkyRoute.Application.Contracts;
using SkyRoute.Application.Data;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Services;
using SkyRoute.Application.Tests.TestDoubles;
using SkyRoute.Application.Validation;
using SkyRoute.Infrastructure.Tenancy;

namespace SkyRoute.Application.Tests.Services;

/// <summary>
/// Unit tests for BookingService (architecture-plan.md Section 3.3, feature-booking-flow.md
/// Section 5). The SUT is composed with real, dependency-free collaborators
/// (BookingReferenceGenerator, RouteTypeResolver, BookingRequestValidator, DefaultTenantContext)
/// and a hand-written FakeBookingStore/CapturingLogger, per test-strategy.md's stub/fake
/// approach (no mocking library).
/// </summary>
public class BookingServiceTests
{
    private static BookingRequest MakeValidBookingRequest(
        string origin = "LHR",
        string destination = "JFK",
        decimal pricePerPassenger = 287.50m,
        int passengerCount = 2,
        string documentType = "Passport",
        string documentNumber = "AB1234C") => new()
    {
        Flight = new BookingFlightRequest
        {
            Provider = "GlobalAir",
            FlightNumber = "GA101",
            Origin = origin,
            Destination = destination,
            DepartureDateTime = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
            ArrivalDateTime = new DateTime(2026, 8, 1, 17, 30, 0, DateTimeKind.Utc),
            DurationMinutes = 510,
            CabinClass = "Economy",
            BaseFare = 250.00m,
            PricePerPassenger = pricePerPassenger,
        },
        PassengerCount = passengerCount,
        Passengers = Enumerable.Range(0, passengerCount).Select(i => new PassengerRequest
        {
            FullName = $"Passenger {i}",
            Email = $"passenger{i}@example.com",
            DocumentType = documentType,
            DocumentNumber = documentNumber,
        }).ToList(),
    };

    private static BookingService MakeSut(FakeBookingStore store) => new(
        store,
        new DefaultTenantContext(),
        new BookingReferenceGenerator(),
        new RouteTypeResolver(new AirportDataService()),
        new BookingRequestValidator(),
        new CapturingLogger<BookingService>());

    [Fact]
    public async Task CreateBookingAsync_InternationalRoute_HappyPath_ReturnsExpectedResponse()
    {
        var store = new FakeBookingStore();
        var sut = MakeSut(store);
        var request = MakeValidBookingRequest(
            origin: "LHR", destination: "JFK", pricePerPassenger: 287.50m, passengerCount: 2,
            documentType: "Passport", documentNumber: "AB1234C");
        var before = DateTime.UtcNow;

        var response = await sut.CreateBookingAsync(request, CancellationToken.None);

        Assert.Equal(575.00m, response.TotalPrice);
        Assert.Matches("^SKY-INT-[A-Z0-9]{6}$", response.BookingReference);
        Assert.Equal(2, response.Passengers.Count);
        Assert.True(Math.Abs((DateTime.UtcNow - response.CreatedAtUtc).TotalSeconds) < 5);
        Assert.True(Math.Abs((response.CreatedAtUtc - before).TotalSeconds) < 5);
    }

    [Fact]
    public async Task CreateBookingAsync_DomesticRoute_HappyPath_ReferenceUsesDomPrefix()
    {
        var store = new FakeBookingStore();
        var sut = MakeSut(store);
        var request = MakeValidBookingRequest(
            origin: "MAN", destination: "LHR", pricePerPassenger: 60.00m, passengerCount: 1,
            documentType: "National ID", documentNumber: "AB-1234");

        var response = await sut.CreateBookingAsync(request, CancellationToken.None);

        Assert.Matches("^SKY-DOM-[A-Z0-9]{6}$", response.BookingReference);
    }

    [Fact]
    public async Task CreateBookingAsync_TotalPrice_IsServerRecomputedFromPricePerPassengerTimesCount()
    {
        // BR-006: proves server-side recomputation for a non-trivial passenger count. There is
        // no client-submitted total in BookingFlightRequest to "override" (AD-004/AD-005).
        var store = new FakeBookingStore();
        var sut = MakeSut(store);
        var request = MakeValidBookingRequest(pricePerPassenger: 115.00m, passengerCount: 3);

        var response = await sut.CreateBookingAsync(request, CancellationToken.None);

        Assert.Equal(345.00m, response.TotalPrice);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public async Task CreateBookingAsync_PassengerCountBoundaries_ReturnsOnePassengerRecordPerSubmission(int passengerCount)
    {
        // BR-005/NFR-DATA-003: passenger record integrity across the boundary counts.
        var store = new FakeBookingStore();
        var sut = MakeSut(store);
        var request = MakeValidBookingRequest(passengerCount: passengerCount);

        var response = await sut.CreateBookingAsync(request, CancellationToken.None);

        Assert.Equal(passengerCount, response.Passengers.Count);
        for (var i = 0; i < passengerCount; i++)
        {
            Assert.Equal($"Passenger {i}", response.Passengers[i].FullName);
        }
    }

    [Fact]
    public async Task CreateBookingAsync_DocumentTypeMismatchAgainstResolvedRoute_ThrowsBookingValidationException()
    {
        var store = new FakeBookingStore();
        var sut = MakeSut(store);
        // LHR -> JFK resolves to International, but the passenger submits a National ID.
        var request = MakeValidBookingRequest(
            origin: "LHR", destination: "JFK", documentType: "National ID", documentNumber: "AB-1234");

        var exception = await Assert.ThrowsAsync<BookingValidationException>(
            () => sut.CreateBookingAsync(request, CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("passengers[0].documentType"));
    }

    [Fact]
    public async Task CreateBookingAsync_ReferenceCollision_RetriesUntilAUniqueReferenceIsFound()
    {
        // BR-004/NFR-DATA-001, Gap-fill BF-03, code review finding CR-003: the fake forces
        // CreateAsync itself (not just the ExistsAsync pre-check) to throw
        // DuplicateBookingReferenceException for the first 3 calls regardless of candidate
        // string, proving the retry loop is driven by catching that exception from CreateAsync
        // — the actual source of truth for uniqueness — rather than relying solely on the
        // preceding ExistsAsync fast-path check.
        var store = new FakeBookingStore { ForceCollisionForFirstNCalls = 3 };
        var sut = MakeSut(store);
        var request = MakeValidBookingRequest();

        var response = await sut.CreateBookingAsync(request, CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(response.BookingReference));
        Assert.True(store.CreateAsyncCallCount > 1);
    }

    [Fact]
    public async Task CreateBookingAsync_ReferenceCollisionNeverResolves_ThrowsAfterExactlyTenAttempts()
    {
        // Gap-fill BF-03, code review finding CR-003: retry cap exhausted after
        // MaxReferenceGenerationAttempts (10) — every CreateAsync call collides via
        // DuplicateBookingReferenceException, driving the retry loop rather than ExistsAsync.
        var store = new FakeBookingStore { AlwaysCollide = true };
        var sut = MakeSut(store);
        var request = MakeValidBookingRequest();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateBookingAsync(request, CancellationToken.None));

        Assert.Equal(10, store.CreateAsyncCallCount);
    }
}
