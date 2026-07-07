namespace SkyRoute.Application.Domain;

/// <summary>
/// One passenger's persisted booking record (BR-005). Plain POCO — no ORM/serialization
/// annotations (DP-PERSIST-002).
/// </summary>
public sealed class PassengerDetail
{
    public required string FullName { get; init; }

    /// <summary>Whole number 0–120, validated by BookingRequestValidator before construction
    /// (PO age feature 2026-07-08). Pure data capture — no business rule is bound to age
    /// (DEC-022, PO 2026-07-08).</summary>
    public required int Age { get; init; }

    public required string Email { get; init; }

    public required string DocumentType { get; init; }

    public required string DocumentNumber { get; init; }
}
