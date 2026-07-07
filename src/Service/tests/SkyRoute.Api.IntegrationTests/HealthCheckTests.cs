using System.Net;
using System.Text.Json;
using SkyRoute.Api.IntegrationTests.TestDoubles;

namespace SkyRoute.Api.IntegrationTests;

/// <summary>
/// Health check endpoint tests (PO directive 2026-07-08): GET /api/health reports overall
/// service health as JSON, aggregating the liveness, booking-store roundtrip, and
/// flight-provider registration checks.
/// </summary>
public class HealthCheckTests : IClassFixture<SkyRouteApiFactory>
{
    private readonly SkyRouteApiFactory _factory;

    public HealthCheckTests(SkyRouteApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Returns200_WithHealthyStatus_AndAllThreeChecks()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());

        var checkNames = root.GetProperty("checks")
            .EnumerateArray()
            .Select(check => check.GetProperty("name").GetString())
            .ToArray();

        Assert.Contains("self", checkNames);
        Assert.Contains("booking-store", checkNames);
        Assert.Contains("flight-providers", checkNames);

        // Every individual check must itself be Healthy in the default test host
        // (two providers registered, in-memory store reachable).
        Assert.All(
            root.GetProperty("checks").EnumerateArray(),
            check => Assert.Equal("Healthy", check.GetProperty("status").GetString()));
    }
}
