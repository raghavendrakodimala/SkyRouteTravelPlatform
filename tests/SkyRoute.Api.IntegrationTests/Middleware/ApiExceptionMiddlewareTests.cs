using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SkyRoute.Api.Middleware;

namespace SkyRoute.Api.IntegrationTests.Middleware;

/// <summary>
/// Unit tests for ApiExceptionMiddleware (BR-011, DP-007, FR-069, NFR-SEC-002). Exercised
/// directly against a DefaultHttpContext rather than through a full WebApplicationFactory,
/// since it is a plain middleware class with no ASP.NET Core hosting dependency beyond
/// RequestDelegate/HttpContext/ILogger.
/// </summary>
public class ApiExceptionMiddlewareTests
{
    private static async Task<(HttpContext Context, string Body)> InvokeWithThrowingNextAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw exception;
        var middleware = new ApiExceptionMiddleware(next, NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        return (context, body);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_Returns500StatusCode()
    {
        var (context, _) = await InvokeWithThrowingNextAsync(new InvalidOperationException("boom"));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_SetsJsonContentType()
    {
        // QA-002 (see phase-13 handoff): ApiExceptionMiddleware assigns
        // context.Response.ContentType = "application/problem+json" (line 43) but then calls
        // context.Response.WriteAsJsonAsync(body) without a contentType argument (line 53).
        // HttpResponseJsonExtensions.WriteAsJsonAsync unconditionally overwrites
        // response.ContentType with the ASP.NET Core default ("application/json; charset=
        // utf-8") whenever no explicit contentType is passed, so the earlier
        // "application/problem+json" assignment is dead code — the response actually observed
        // by clients is "application/json; charset=utf-8", not "application/problem+json".
        // This assertion documents the actual (buggy) behavior; do not fix ApiExceptionMiddleware
        // in this phase.
        var (context, _) = await InvokeWithThrowingNextAsync(new InvalidOperationException("boom"));

        Assert.StartsWith("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_BodyContainsGenericTitleAndStatus_NoExceptionDetail()
    {
        var (_, body) = await InvokeWithThrowingNextAsync(new InvalidOperationException("boom"));

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(
            "An unexpected error occurred. Please try again later.",
            root.GetProperty("title").GetString());
        Assert.Equal(500, root.GetProperty("status").GetInt32());

        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("boom", body);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_DoesNotItselfThrow()
    {
        // context.Response.HasStarted is false at invocation time here (no writes have
        // happened yet), so the exception must be fully handled, not rethrown.
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = _ => throw new InvalidOperationException("boom");
        var middleware = new ApiExceptionMiddleware(next, NullLogger<ApiExceptionMiddleware>.Instance);

        Assert.False(context.Response.HasStarted);
        await middleware.InvokeAsync(context);
    }
}
