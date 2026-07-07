namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Named cabin-class fare multipliers applied to a flight's Economy base fare before the
/// provider's own pricing rule (BR-001/BR-002) is applied (feature-provider-aggregation.md
/// Section 4, Gap-fill PA-02). Shared by both providers to avoid duplicating the multiplier
/// table (DRY, NFR-MAINT-002).
/// </summary>
internal static class CabinClassMultipliers
{
    public const decimal Economy = 1.0m;
    public const decimal Business = 2.0m;
    public const decimal FirstClass = 3.5m;

    public static decimal ForCabinClass(string cabinClass) => cabinClass switch
    {
        "Economy" => Economy,
        "Business" => Business,
        "First Class" => FirstClass,
        _ => Economy,
    };
}
