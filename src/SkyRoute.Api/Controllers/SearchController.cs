using Microsoft.AspNetCore.Mvc;
using SkyRoute.Application.Contracts;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Validation;

namespace SkyRoute.Api.Controllers;

/// <summary>
/// Thin POST /api/search controller (DP-003/DP-004 analog, DP-005). Contains no
/// aggregation, provider, or business logic — validates, then delegates to
/// IFlightAggregatorService.
/// </summary>
[ApiController]
[Route("api/search")]
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
    public async Task<IActionResult> Search([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        var errors = _validator.Validate(request);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors.ToModelState());
        }

        var results = await _aggregator.SearchAsync(request, cancellationToken);
        return Ok(results);
    }
}
