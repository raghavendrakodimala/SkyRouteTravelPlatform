using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.Application.Dtos;

namespace SkyRoute.Api.IntegrationTests.Controllers;

/// <summary>
/// Full-stack integration tests for POST /api/v1/bookings (architecture-plan.md Section 5,
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

    private static BookingFlightRequest MakeFlight(
        string origin,
        string destination,
        decimal pricePerPassenger,
        string flightNumber = "GA101",
        decimal baseFare = 250.00m) => new()
    {
        Provider = "GlobalAir",
        FlightNumber = flightNumber,
        Origin = origin,
        Destination = destination,
        DepartureDateTime = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
        ArrivalDateTime = new DateTime(2026, 8, 1, 17, 30, 0, DateTimeKind.Utc),
        DurationMinutes = 510,
        CabinClass = "Economy",
        BaseFare = baseFare,
        PricePerPassenger = pricePerPassenger,
    };

    private static PassengerRequest MakePassenger(
        string fullName, string email, string documentType, string documentNumber, int age = 34) => new()
    {
        FullName = fullName,
        Age = age,
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
                MakePassenger("Jane Doe", "jane@example.com", "Passport", "AB1234C", age: 34),
                MakePassenger("John Smith", "john@example.com", "Passport", "CD5678E", age: 8),
            },
        };

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var parsed = JsonSerializer.Deserialize<BookingResponseDto>(body, CaseInsensitiveOptions);
        Assert.NotNull(parsed);
        Assert.Matches("^SKY-INT-[A-Z0-9]{6}$", parsed!.BookingReference);
        Assert.Equal(575.00m, parsed.TotalPrice);
        Assert.Equal(2, parsed.Passengers.Count);

        // Data-minimization rule, feature-booking-flow.md Section 4: only fullName and age
        // (PO age feature 2026-07-08) are present — email/document data is never echoed back.
        using var document = JsonDocument.Parse(body);
        var passengersElement = document.RootElement.GetProperty("passengers");
        var echoedAges = new List<int>();
        foreach (var passenger in passengersElement.EnumerateArray())
        {
            var propertyNames = passenger.EnumerateObject().Select(p => p.Name).ToList();
            Assert.Contains("fullName", propertyNames);
            Assert.Contains("age", propertyNames);
            Assert.DoesNotContain("email", propertyNames);
            Assert.DoesNotContain("documentNumber", propertyNames);
            Assert.DoesNotContain("documentType", propertyNames);
            echoedAges.Add(passenger.GetProperty("age").GetInt32());
        }

        // Ages echo back in submission order (any 0–120 age is legal at any position —
        // DEC-022: age is pure data capture, no business rule bound to it).
        Assert.Equal(new[] { 34, 8 }, echoedAges);

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

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("passengerCount"));
    }

    [Fact]
    public async Task CreateBooking_DocumentRouteMismatch_Returns400ViaValidationProblem_Not500()
    {
        // GA412 (MAN -> LHR) resolves to Domestic server-side, but the passenger submits a
        // Passport. AUD-025/028/033: this now uses a GENUINELY domestic flight (GA412, base 80.00,
        // per-passenger 92.00) so the route/fare snapshot validates and the request reaches the
        // BR-003 document-type check — the previous version declared GA101 (a real international
        // flight) on MAN->LHR, which the route re-resolution now rejects before documents.
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("MAN", "LHR", 92.00m, flightNumber: "GA412", baseFare: 80.00m),
            PassengerCount = 1,
            Passengers = new List<PassengerRequest>
            {
                MakePassenger("Jane Doe", "jane@example.com", "Passport", "AB1234C"),
            },
        };

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(problem!.Errors.Keys, k => k.StartsWith("passengers[0].documentType"));
    }

    [Fact]
    public async Task CreateBooking_InternationalFlightDeclaredDomestic_Returns400OnRoute_NotConfirmedOnNationalId()
    {
        // AUD-025/028/033 end-to-end: GA204 is genuinely LHR->DXB (international). Declaring it as
        // LHR->MAN (a same-country UK domestic pair) with only a National ID was the passport-gate
        // bypass. The server re-resolves the real route and rejects it (flight.destination) — the
        // booking is NOT confirmed as a domestic National-ID itinerary.
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("LHR", "MAN", 345.00m, flightNumber: "GA204", baseFare: 300.00m),
            PassengerCount = 1,
            Passengers = new List<PassengerRequest>
            {
                MakePassenger("Jane Doe", "jane@example.com", "National ID", "AB-12345"),
            },
        };

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("flight.destination"));
    }

    [Fact]
    public async Task CreateBooking_InternationalFlightCorrectlyDeclared_Returns201WithIntReference()
    {
        // AUD-025/028/033 positive counterpart: GA204 correctly declared as LHR->DXB with a
        // Passport books normally as SKY-INT — a correct international itinerary is unaffected.
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("LHR", "DXB", 345.00m, flightNumber: "GA204", baseFare: 300.00m),
            PassengerCount = 1,
            Passengers = new List<PassengerRequest>
            {
                MakePassenger("Jane Doe", "jane@example.com", "Passport", "AB1234C"),
            },
        };

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var parsed = JsonSerializer.Deserialize<BookingResponseDto>(body, CaseInsensitiveOptions);
        Assert.NotNull(parsed);
        Assert.Matches("^SKY-INT-[A-Z0-9]{6}$", parsed!.BookingReference);
    }

    [Fact]
    public async Task CreateBooking_EmptyBody_Returns400_Not500()
    {
        // AUD-027: an empty body binds the model to null under SuppressModelStateInvalidFilter —
        // must be a 400 problem, not a 500.
        var client = _factory.CreateHttpsClient();
        using var content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/bookings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_MalformedJsonBody_Returns400_Not500()
    {
        // AUD-027: truncated/malformed JSON.
        var client = _factory.CreateHttpsClient();
        using var content = new StringContent("{ \"flight\": {", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/bookings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_OversizedPassengersArray_Returns400WithPassengersError()
    {
        // AUD-034: a small passengerCount alongside an oversized passengers[] must be rejected on
        // the array bound (400) — proving the per-passenger loop is not walked for the whole
        // oversized collection.
        var client = _factory.CreateHttpsClient();
        var request = new BookingRequest
        {
            Flight = MakeFlight("LHR", "JFK", 287.50m),
            PassengerCount = 2,
            Passengers = Enumerable.Range(0, 100).Select(_ => new PassengerRequest()).ToList(),
        };

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("passengers"));
        Assert.DoesNotContain(problem.Errors.Keys, k => k.StartsWith("passengers[", StringComparison.Ordinal));
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

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("flight.pricePerPassenger"));
    }

    [Fact]
    public async Task CreateBooking_ExplicitNullPassengers_Returns400ValidationProblem_Not500()
    {
        // QA-001 (Phase 14 test execution summary): a crafted body sending "passengers": null
        // (overriding BookingRequest's property initializer default — not producible via the
        // real frontend, only via a raw HTTP client) previously reached code that assumed a
        // non-null collection and surfaced as an unhandled 500. It must be a clean 400
        // validation problem with field-level errors, never a 500.
        var client = _factory.CreateHttpsClient();
        const string rawJson = """
            {
              "flight": {
                "provider": "GlobalAir",
                "flightNumber": "GA101",
                "origin": "LHR",
                "destination": "JFK",
                "departureDateTime": "2026-08-01T09:00:00Z",
                "arrivalDateTime": "2026-08-01T17:30:00Z",
                "durationMinutes": 510,
                "cabinClass": "Economy",
                "baseFare": 250.00,
                "pricePerPassenger": 287.50
              },
              "passengerCount": 1,
              "passengers": null
            }
            """;

        using var content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/bookings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        // Null passengers with passengerCount 1 is reported as a field-level error against the
        // passenger data (exact key depends on which validation layer catches it first — either
        // the framework's implicit required check on "passengers"/"Passengers" or
        // BookingRequestValidator's "passengerCount" mismatch); what matters is a clean,
        // field-addressed 400 rather than an unhandled 500.
        Assert.NotEmpty(problem!.Errors);
        Assert.Contains(problem.Errors.Keys, k => k.Contains("assenger"));
    }

    [Fact]
    public async Task CreateBooking_ExplicitNullFlightAndNullPassengers_Returns400ValidationProblem_Not500()
    {
        // QA-001 sibling case: every nullable top-level member explicitly nulled at once.
        var client = _factory.CreateHttpsClient();
        const string rawJson = """{ "flight": null, "passengerCount": 1, "passengers": null }""";

        using var content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/bookings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.NotEmpty(problem!.Errors);
    }

    [Fact]
    public async Task CreateBooking_RawJsonWithMissingAge_Returns400WithAgeFieldError()
    {
        // PO age feature 2026-07-08: a raw body that simply omits "age" (a legacy/pre-age
        // client shape) must fail structural validation with a field-addressed 400 on
        // passengers[0].age — never a 500, and never a silently-defaulted age of 0.
        var client = _factory.CreateHttpsClient();
        const string rawJson = """
            {
              "flight": {
                "provider": "GlobalAir",
                "flightNumber": "GA101",
                "origin": "LHR",
                "destination": "JFK",
                "departureDateTime": "2026-08-01T09:00:00Z",
                "arrivalDateTime": "2026-08-01T17:30:00Z",
                "durationMinutes": 510,
                "cabinClass": "Economy",
                "baseFare": 250.00,
                "pricePerPassenger": 287.50
              },
              "passengerCount": 1,
              "passengers": [
                { "fullName": "Jane Doe", "email": "jane@example.com", "documentType": "Passport", "documentNumber": "AB1234C" }
              ]
            }
            """;

        using var content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/bookings", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(
            new[] { "Age is required and must be a whole number between 0 and 120." },
            problem!.Errors["passengers[0].age"]);
    }

    [Fact]
    public async Task CreateBooking_HappyPath_ResponseHasNoLocationHeader()
    {
        // Gap-fill BF-05: no GET /api/v1/bookings/{reference} endpoint exists, so no
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

        var response = await client.PostAsJsonAsync("/api/v1/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
}
