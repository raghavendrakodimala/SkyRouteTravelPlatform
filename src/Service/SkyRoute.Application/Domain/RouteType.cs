namespace SkyRoute.Application.Domain;

/// <summary>
/// Domestic vs. international route classification (BR-003). Determined authoritatively
/// server-side by RouteTypeResolver, never trusted from client input (DP-016).
/// </summary>
public enum RouteType
{
    Domestic,
    International
}
