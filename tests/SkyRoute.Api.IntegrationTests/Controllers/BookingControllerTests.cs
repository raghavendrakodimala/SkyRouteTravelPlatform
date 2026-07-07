using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.Application.Contracts;

namespace SkyRoute.Api.IntegrationTests.Controllers;

/// <summary>
/// Full-stack integration tests for POST /api/bookings (architecture-plan.md Section 5,
/// feature-booking-flow.md Sections 3-4). Exact JSON shapes referenced here are documented in
/// feature-booking-flow.md Sections 3-4; the full field-level validation rule matrix is
/// already covered at the unit level by BookingRequestValidatorTests/BookingServiceTests.
/// </summary>
public class BookingControllerTests : IClassFixture<SkyRouteApiFactory>
{
    private readonly SkyRouteApiFactory _factory;

    public BookingControllerTests(SkyRouteApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record BookingResponseDto(
        string BookingReference,
        object Flight,
        decimal TotalPrice,
        List<Dictionary<string, object>> Passengers,
        DateTime CreatedAtUtc);

    private static BookingFlightRequest MakeFlight(string origin, string destination, decimal pricePerPassenger) => new()
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
    };

    private static PassengerRequest MakePassenger(
        string fullName, string email, string documentType, string documentNumber) => new()
    {
        FullName = fullName,
        Email = email,
        DocumentType = documentType,
        DocumentNumber = documentNumber,
    };

    [Fact]
    public async Task CreateBooking_InternationalHappyPath_Returns201WithDataMinimizedPassengers()
    {
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("LHR", "JFK", 287.50m),
            PassengerCount = 2,
            Passengers = new List<PassengerRequest>
            {
                MakePassenger("Jane Doe", "jane@example.com", "Passport", "AB1234C"),
                MakePassenger("John Smith", "john@example.com", "Passport", "CD5678E"),
            },
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var parsed = JsonSerializer.Deserialize<BookingResponseDto>(body, CaseInsensitiveOptions);
        Assert.NotNull(parsed);
        Assert.Matches("^SKY-INT-[A-Z0-9]{6}$", parsed!.BookingReference);
        Assert.Equal(575.00m, parsed.TotalPrice);
        Assert.Equal(2, parsed.Passengers.Count);

        // Data-minimization rule, feature-booking-flow.md Section 4: only fullName is present.
        using var document = JsonDocument.Parse(body);
        var passengersElement = document.RootElement.GetProperty("passengers");
        foreach (var passenger in passengersElement.EnumerateArray())
        {
            var propertyNames = passenger.EnumerateObject().Select(p => p.Name).ToList();
            Assert.Contains("fullName", propertyNames);
            Assert.DoesNotContain("email", propertyNames);
            Assert.DoesNotContain("documentNumber", propertyNames);
            Assert.DoesNotContain("documentType", propertyNames);
        }

        Assert.DoesNotContain("email", body);
        Assert.DoesNotContain("documentNumber", body);
    }

    [Fact]
    public async Task CreateBooking_PassengerCountMismatch_Returns400WithPassengerCountError()
    {
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("LHR", "JFK", 287.50m),
            PassengerCount = 2,
            Passengers = new List<PassengerRequest>
            {
                MakePassenger("Jane Doe", "jane@example.com", "Passport", "AB1234C"),
            },
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("passengerCount"));
    }

    [Fact]
    public async Task CreateBooking_DocumentRouteMismatch_Returns400ViaValidationProblem_Not500()
    {
        // MAN -> LHR resolves to Domestic server-side, but the passenger submits a Passport.
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("MAN", "LHR", 60.00m),
            PassengerCount = 1,
            Passengers = new List<PassengerRequest>
            {
                MakePassenger("Jane Doe", "jane@example.com", "Passport", "AB1234C"),
            },
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(problem!.Errors.Keys, k => k.StartsWith("passengers[0].documentType"));
    }

    [Fact]
    public async Task CreateBooking_FabricatedPricePerPassenger_Returns400WithFlightPriceError()
    {
        // SEC-001 (Phase 16 security review): same provider/route/passenger inputs as
        // CreateBooking_InternationalHappyPath_Returns201WithDataMinimizedPassengers, but with
        // a fabricated PricePerPassenger ($0.01 instead of GA101/Economy's real 287.50 fare).
        // Previously this was internally consistent (positive, valid cabin class) and would
        // have been trusted end-to-end into a confirmed 201 booking; it must now be rejected.
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("LHR", "JFK", 0.01m),
            PassengerCount = 2,
            Passengers = new List<PassengerRequest>
            {
                MakePassenger("Jane Doe", "jane@example.com", "Passport", "AB1234C"),
                MakePassenger("John Smith", "john@example.com", "Passport", "CD5678E"),
            },
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("flight.pricePerPassenger"));
    }

    [Fact]
    public async Task CreateBooking_HappyPath_ResponseHasNoLocationHeader()
    {
        // Gap-fill BF-05: no GET /api/bookings/{reference} endpoint exists, so no
        // CreatedAtAction/Location header is used.
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("LHR", "JFK", 287.50m),
            PassengerCount = 1,
            Passengers = new List<PassengerRequest>
            {
                MakePassenger("Jane Doe", "jane@example.com", "Passport", "AB1234C"),
            },
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
}
