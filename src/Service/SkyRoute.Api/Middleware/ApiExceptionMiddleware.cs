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
        catch (BadHttpRequestException ex)
        {
            // AUD-027: a request body the server could not even read/parse (e.g. malformed JSON
            // or a body-read failure that surfaces as an exception rather than a null-bound
            // model) is a CLIENT error — map it to a 400 problem+json in the same contract, not a
            // 500. The controllers' own null-body guard handles the common formatter-failure path;
            // this branch is the defensive net for any bad-request exception that propagates here.
            _logger.LogWarning(ex, "Malformed request for {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var badRequestBody = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "The request body could not be read. Please send a valid JSON request body.",
                status = StatusCodes.Status400BadRequest,
                traceId = context.TraceIdentifier,
            };

            await context.Response.WriteAsJsonAsync(badRequestBody, options: null, contentType: "application/problem+json");
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

            var body = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "An unexpected error occurred. Please try again later.",
                status = StatusCodes.Status500InternalServerError,
                traceId = context.TraceIdentifier,
            };

            // QA-002: the content type must be passed to WriteAsJsonAsync directly — assigning
            // context.Response.ContentType beforehand is dead code, because WriteAsJsonAsync
            // without an explicit contentType argument unconditionally overwrites the header
            // with the ASP.NET Core default ("application/json; charset=utf-8"). Passing it
            // here is what makes clients actually observe the RFC 7807 problem+json type.
            await context.Response.WriteAsJsonAsync(body, options: null, contentType: "application/problem+json");
        }
    }
}
