using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.Extensions;
using SkyRoute.Application.Dtos;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Validation;

namespace SkyRoute.Api.Controllers;

/// <summary>
/// Thin POST /api/search controller (DP-003/DP-004 analog, DP-005). Contains no
/// aggregation, provider, or business logic — validates, then delegates to
/// IFlightAggregatorService.
/// </summary>
[ApiController]
[ApiVersion("1.0")] // mapped to the default v1 version (PO directive 2026-07-08)
[Route("search")] // global "api" prefix applied by GlobalRoutePrefixConvention (Program.cs)
public sealed class SearchController : ControllerBase
{
    private readonly IFlightAggregatorService _aggregator;
    private readonly SearchRequestValidator _validator;

    public SearchController(IFlightAggregatorService aggregator, SearchRequestValidator validator)
    {
        _aggregator = aggregator;
        _validator = validator;
    }

    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<FlightResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Search([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        // AUD-027: with SuppressModelStateInvalidFilter (Program.cs) the framework does not
        // auto-400 a body it could not bind (empty/null/malformed/wrong-typed JSON) — the model
        // arrives null. Return the same field-keyed 400 contract instead of letting a downstream
        // null dereference surface as a 500.
        if (request is null)
        {
            return ValidationProblem(InvalidBodyErrors().ToModelState());
        }

        var errors = _validator.Validate(request);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors.ToModelState());
        }

        var result = await _aggregator.SearchAsync(request, cancellationToken);

        // AUD-038: distinguish a total provider outage (every provider failed) from a genuine
        // no-match. Total outage → 503 problem+json so the UI can prompt a retry and monitoring
        // can alert; a healthy no-match stays 200 with an empty list; partial failure stays 200
        // with the available results (fault isolation preserved).
        if (result.AllProvidersFailed)
        {
            return Problem(
                title: "Flight search is temporarily unavailable. Please try again shortly.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(result.Flights);
    }

    private static Dictionary<string, string[]> InvalidBodyErrors() => new()
    {
        ["request"] = ["A valid JSON request body is required."],
    };
}
