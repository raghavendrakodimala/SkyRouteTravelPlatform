using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SkyRoute.Api.Controllers;

/// <summary>
/// Shared helper for converting a field-error dictionary (as produced by request
/// validators) into a <see cref="ModelStateDictionary"/> suitable for
/// <c>ControllerBase.ValidationProblem(...)</c>. Extracted from <see cref="SearchController"/>
/// and <see cref="BookingController"/> (CR-001) to avoid duplicating this mapping logic
/// in every controller that needs to return a validation problem response.
/// </summary>
public static class ValidationProblemExtensions
{
    public static ModelStateDictionary ToModelState(this IDictionary<string, string[]> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var (field, messages) in errors)
        {
            foreach (var message in messages)
            {
                modelState.AddModelError(field, message);
            }
        }

        return modelState;
    }
}
