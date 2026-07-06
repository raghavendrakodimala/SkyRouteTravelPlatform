namespace SkyRoute.Api.Middleware;

/// <summary>
/// The single, centralized exception-handling location for the entire application
/// (BR-011, DP-007). Registered first in the pipeline (Program.cs), before routing, so it
/// wraps every downstream request. Produces the generic 500 body from
/// feature-error-handling-and-validation.md Section 1.3 (Gap-fill EH-01) — never a stack
/// trace, exception type, or internal message in the response (FR-069, NFR-SEC-002).
/// Does not handle 400s (those are ValidationProblem results returned directly by
/// controllers, not exceptions) or provider-level exceptions (those are caught and
/// neutralized inside FlightAggregatorService.SafeInvokeAsync before they could ever reach
/// this middleware).
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Server-side log only may include exception detail; the response body never does.
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var body = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "An unexpected error occurred. Please try again later.",
                status = StatusCodes.Status500InternalServerError,
                traceId = context.TraceIdentifier,
            };

            await context.Response.WriteAsJsonAsync(body);
        }
    }
}
