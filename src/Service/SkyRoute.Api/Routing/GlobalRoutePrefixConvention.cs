using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace SkyRoute.Api.Routing;

/// <summary>
/// Applies a single global route prefix to every attribute-routed controller (PO directive
/// 2026-07-08): controllers declare only their resource segment (e.g. <c>[Route("search")]</c>)
/// and the prefix lives in exactly one place — <c>Program.cs</c>. Changing the public prefix
/// (e.g. to <c>api/v1</c> when versioning is introduced) is a one-line change there, with no
/// per-controller edits.
/// </summary>
public sealed class GlobalRoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix;

    public GlobalRoutePrefixConvention(string routePrefix)
    {
        _prefix = new AttributeRouteModel(new Microsoft.AspNetCore.Mvc.RouteAttribute(routePrefix));
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is null
                    ? _prefix
                    : AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel);
            }
        }
    }
}
