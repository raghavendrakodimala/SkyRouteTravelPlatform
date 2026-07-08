namespace SkyRoute.Application.Domain;

/// <summary>
/// Semantic helpers for <see cref="RouteType"/> exposed as C# 14 extension members. Centralizes
/// the "which enum value means international" decision (BR-003) that BookingReferenceGenerator
/// (reference prefix) and BookingRequestValidator (document-format selection) otherwise each
/// restate as a raw <c>== RouteType.International</c> comparison. Pure, allocation-free read —
/// no behavior change, it is exactly that comparison behind a name.
/// </summary>
public static class RouteTypeExtensions
{
    extension(RouteType routeType)
    {
        /// <summary>True when the route crosses a country boundary (BR-003) — the stricter,
        /// passport-required case that also drives the SKY-INT-… reference prefix (BR-004).</summary>
        public bool IsInternational => routeType == RouteType.International;
    }
}
