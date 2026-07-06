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
/// Full-stack integration tests for POST /api/search via WebApplicationFactory&lt;Program&gt;
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

    [Fact]
    public async Task Search_ValidRequest_Returns200WithEightMergedResultsFromBothProviders()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/search", MakeValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<List<FlightResultDto>>(body, CaseInsensitiveOptions);

        Assert.NotNull(results);
        Assert.Equal(8, results!.Count);
        Assert.All(results, r => Assert.Contains(r.Provider, new[] { "GlobalAir", "BudgetWings" }));
        Assert.Equal(4, results.Count(r => r.Provider == "GlobalAir"));
        Assert.Equal(4, results.Count(r => r.Provider == "BudgetWings"));
    }

    [Fact]
    public async Task Search_OriginEqualsDestination_Returns400WithDestinationError()
    {
        var client = _factory.CreateHttpsClient();
        var request = MakeValidRequest();
        request.Destination = request.Origin;

        var response = await client.PostAsJsonAsync("/api/search", request);

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

        var response = await client.PostAsJsonAsync("/api/search", MakeValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<List<FlightResultDto>>(body, CaseInsensitiveOptions);

        Assert.NotNull(results);
        Assert.Equal(4, results!.Count);
        Assert.All(results, r => Assert.Equal("GlobalAir", r.Provider));
        Assert.DoesNotContain("BudgetWings", body);
    }
}
