namespace SkyRoute.Application.Validation;

/// <summary>
/// Named cabin-class allow-list (DP-015 pattern). Single source of truth for the set of valid
/// cabin class values, referenced by both SearchRequestValidator (POST /api/search) and
/// BookingRequestValidator (POST /api/bookings, SEC-001 fix) so the two endpoints enforce the
/// same business rule. Never duplicated inline in another class.
/// </summary>
public static class CabinClasses
{
    /// <summary>The only cabin class values accepted anywhere in the API (FR-/BR- search + booking flows).</summary>
    public static readonly string[] ValidCabinClasses = { "Economy", "Business", "First Class" };
}
