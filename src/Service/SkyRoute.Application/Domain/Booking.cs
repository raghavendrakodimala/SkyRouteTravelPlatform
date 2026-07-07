namespace SkyRoute.Application.Domain;

/// <summary>
/// A persisted booking record (FR-043, BR-005, BR-006, BR-008). Plain POCO — no
/// ORM/serialization/schema annotations (DP-PERSIST-002, DP-PROTOCOL-002). Carries
/// <see cref="TenantId"/> per the multi-tenancy seam (DP-TENANT-005), always "default" in MVP.
/// </summary>
public sealed class Booking
{
    public required string BookingReference { get; init; }

    public required BookingFlightSnapshot Flight { get; init; }

    public required IReadOnlyList<PassengerDetail> Passengers { get; init; }

    public required string CabinClass { get; init; }

    public required decimal TotalPrice { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public string TenantId { get; init; } = "default";
}
