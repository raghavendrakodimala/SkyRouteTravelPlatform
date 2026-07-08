using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkyRoute.Api.IntegrationTests.TestDoubles;
using SkyRoute.Application.Contracts;
using SkyRoute.Application.Interfaces;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Api.IntegrationTests.Controllers;

/// <summary>
/// Full-stack integration tests for POST /api/v1/search via WebApplicationFactory&lt;Program&gt;
/// (test-strategy.md Section 6 — the required automated provider fault-isolation scenario at
/// the integration level, plus happy-path and one representative validation case; the full
/// validation rule matrix is already covered at the unit level by SearchRequestValidatorTests).
/// </summary>
public class SearchControllerTests : IClassFixture<SkyRouteApiFactory>
{
    private readonly SkyRouteApiFactory _factory;

    public SearchControllerTests(SkyRouteApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record FlightResultDto(
        string Provider,
        string FlightNumber,
        string Origin,
        string Destination,
        DateTime DepartureDateTime,
        DateTime ArrivalDateTime,
        int DurationMinutes,
        string CabinClass,
        decimal BaseFare,
        decimal PricePerPassenger);

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    private static SearchRequest MakeValidRequest() => new()
    {
        Origin = "LHR",
        Destination = "JFK",
        DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
        PassengerCount = 2,
        CabinClass = "Economy",
        TripType = "OneWay",
    };

    /// <summary>
    /// Route-filtering fix (requirements-compliance follow-up, reversing ASM-006/OQ-003):
    /// each provider's fixed schedule is now filtered to the requested route before
    /// merging, so LHR-&gt;JFK (MakeValidRequest) yields exactly the one matching flight
    /// from each provider (GA101, BW210) rather than each provider's full 4-flight
    /// schedule. Still proves both providers were queried and merged (BR-007/FR-049).
    /// </summary>
    [Fact]
    public async Task Search_ValidRequest_Returns200WithResultsFilteredToRequestedRouteFromBothProviders()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/v1/search", MakeValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<List<FlightResultDto>>(body, CaseInsensitiveOptions);

        Assert.NotNull(results);
        Assert.Equal(2, results!.Count);
        Assert.All(results, r => Assert.Contains(r.Provider, new[] { "GlobalAir", "BudgetWings" }));
        Assert.All(results, r => Assert.Equal("LHR", r.Origin));
        Assert.All(results, r => Assert.Equal("JFK", r.Destination));
        Assert.Contains(results, r => r.Provider == "GlobalAir" && r.FlightNumber == "GA101");
        Assert.Contains(results, r => r.Provider == "BudgetWings" && r.FlightNumber == "BW210");
    }

    [Fact]
    public async Task Search_OriginEqualsDestination_Returns400WithDestinationError()
    {
        var client = _factory.CreateHttpsClient();
        var request = MakeValidRequest();
        request.Destination = request.Origin;

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("destination"));
    }

    [Fact]
    public async Task Search_OneProviderThrows_Returns200WithOnlySurvivingProviderResults()
    {
        // BR-007/FR-009/FR-050/FR-070: substitute a throwing IFlightProvider for BudgetWings
        // and assert the request still succeeds with only GlobalAir's results — no partial-
        // failure indicator of any kind in the response shape.
        using var faultyFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IFlightProvider>();
                services.AddScoped<IFlightProvider, GlobalAirProvider>();
                services.AddScoped<IFlightProvider>(_ => new ThrowingFlightProvider(
                    "BudgetWings", new InvalidOperationException("Simulated BudgetWings internal fault")));
            });
        });

        var client = faultyFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var response = await client.PostAsJsonAsync("/api/v1/search", MakeValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<List<FlightResultDto>>(body, CaseInsensitiveOptions);

        // Route-filtering fix: LHR->JFK matches exactly one GlobalAir flight (GA101) in the
        // fixed schedule, not the provider's full 4-flight schedule.
        Assert.NotNull(results);
        var flight = Assert.Single(results!);
        Assert.Equal("GlobalAir", flight.Provider);
        Assert.Equal("GA101", flight.FlightNumber);
        Assert.DoesNotContain("BudgetWings", body);
    }

    [Fact]
    public async Task Search_AllProvidersThrow_Returns503ProblemJson()
    {
        // AUD-038: when EVERY provider fails, the endpoint returns 503 (a total outage), not an
        // empty-but-successful 200 that a genuine no-match would produce.
        using var faultyFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IFlightProvider>();
                services.AddScoped<IFlightProvider>(_ => new ThrowingFlightProvider(
                    "GlobalAir", new InvalidOperationException("Simulated GlobalAir fault")));
                services.AddScoped<IFlightProvider>(_ => new ThrowingFlightProvider(
                    "BudgetWings", new InvalidOperationException("Simulated BudgetWings fault")));
            });
        });

        var client = faultyFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var response = await client.PostAsJsonAsync("/api/v1/search", MakeValidRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        // Assert on the parsed RFC7807 problem body (host-independent) rather than the raw
        // content-type header, which MVC content-negotiates to application/json without an
        // explicit Accept in this test host.
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(503, problem!.Status);
    }

    [Fact]
    public async Task Search_AllProvidersHealthyButNoMatchingRoute_Returns200WithEmptyList()
    {
        // AUD-038: MAN->SYD is the DEC-021 no-direct-service route — both real providers run
        // successfully and return zero flights. That genuine no-match stays 200 [], not 503.
        var client = _factory.CreateHttpsClient();
        var request = MakeValidRequest();
        request.Origin = "MAN";
        request.Destination = "SYD";

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = JsonSerializer.Deserialize<List<FlightResultDto>>(
            await response.Content.ReadAsStringAsync(), CaseInsensitiveOptions);
        Assert.NotNull(results);
        Assert.Empty(results!);
    }

    // AUD-027: an ill-formed request body must be a 400 (RFC7807 problem), never a 500. With
    // SuppressModelStateInvalidFilter the model binds to null on any body-read failure, so the
    // controller's null-guard (and the middleware's BadHttpRequestException net) must catch it.
    [Theory]
    [InlineData("")] // empty body
    [InlineData("null")] // literal JSON null
    [InlineData("{ \"origin\": \"LHR\",")] // malformed / truncated JSON
    [InlineData("{ \"origin\": \"LHR\", \"destination\": \"JFK\", \"departureDate\": \"2026-13-45\", \"passengerCount\": 1, \"cabinClass\": \"Economy\", \"tripType\": \"OneWay\" }")] // invalid date value
    [InlineData("{ \"origin\": \"LHR\", \"destination\": \"JFK\", \"departureDate\": \"2026-08-01\", \"passengerCount\": 2.9, \"cabinClass\": \"Economy\", \"tripType\": \"OneWay\" }")] // fractional int
    public async Task Search_IllFormedBody_Returns400_Not500(string rawJson)
    {
        var client = _factory.CreateHttpsClient();
        using var content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/search", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
