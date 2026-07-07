using System.Net;
using System.Text.Json;
using SkyRoute.Api.IntegrationTests.TestDoubles;

namespace SkyRoute.Api.IntegrationTests;

/// <summary>
/// API documentation endpoint tests (PO directive 2026-07-08, DEC-023): the built-in
/// ASP.NET Core OpenAPI generator serves the OpenAPI 3.1 document, and the Scalar
/// interactive reference is reachable — both under the /api route prefix (PO directive:
/// every route the service exposes lives under /api). WebApplicationFactory hosts run in
/// the Development environment, matching the Development-only mapping in Program.cs.
/// </summary>
public class OpenApiDocumentTests : IClassFixture<SkyRouteApiFactory>
{
    private readonly SkyRouteApiFactory _factory;

    public OpenApiDocumentTests(SkyRouteApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApiDocument_IsServed_UnderApiPrefix_AsOpenApi31_AndDescribesBothEndpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        // Latest supported OpenAPI format (3.1.x) per the PO directive.
        Assert.StartsWith("3.1", root.GetProperty("openapi").GetString());
        Assert.Equal("SkyRoute API", root.GetProperty("info").GetProperty("title").GetString());

        var paths = root.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1/search", out _), "OpenAPI document must describe POST /api/v1/search");
        Assert.True(paths.TryGetProperty("/api/v1/bookings", out _), "OpenAPI document must describe POST /api/v1/bookings");
    }

    [Fact]
    public async Task ScalarApiReference_IsReachable_UnderApiPrefix()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("SkyRoute API Reference", html);
    }

    [Fact]
    public async Task UnprefixedDocumentationRoutes_DoNotExist()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/scalar/v1")).StatusCode);
    }
}
