namespace SkyRoute.Application.Validation;

/// <summary>
/// Named validation pattern constants (DP-015). Referenced by BookingRequestValidator and
/// mirrored (not duplicated) by the Angular document-number.validators.ts. Never duplicated
/// inline in another class.
/// </summary>
public static class DocumentPatterns
{
    /// <summary>International route document format: 6–9 uppercase letters/digits (BR-003, US-005 AC6).</summary>
    public const string PassportPattern = "^[A-Z0-9]{6,9}$";

    /// <summary>Domestic route document format: 5–20 letters/digits/hyphens, either case (Gap-fill BF-04).</summary>
    public const string NationalIdPattern = "^[A-Za-z0-9-]{5,20}$";

    /// <summary>
    /// Email format (FR-065, Gap-fill BF-01). SEC-004 (Phase 16 security review): bounded to a
    /// maximum overall length of 254 characters (RFC 5321 practical max) via a leading
    /// zero-width lookahead, consistent with the explicit numeric bounds already used by
    /// <see cref="PassportPattern"/>/<see cref="NationalIdPattern"/>/<see cref="FullNamePattern"/>
    /// — this caps backtracking cost on oversized input without changing the local-part/
    /// domain-part matching behavior for values within the bound.
    /// </summary>
    public const string EmailPattern = @"^(?=.{1,254}$)[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";

    /// <summary>Full name: 2–100 chars, at least one letter (FR-064, Gap-fill BF-02).</summary>
    public const string FullNamePattern = @"^(?=.*[A-Za-z]).{2,100}$";

    /// <summary>Literal documentType enum value for international bookings (FR-040).</summary>
    public const string PassportDocumentType = "Passport";

    /// <summary>Literal documentType enum value for domestic bookings (FR-040) — note the space.</summary>
    public const string NationalIdDocumentType = "National ID";
}
