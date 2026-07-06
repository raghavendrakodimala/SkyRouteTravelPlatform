using System.ComponentModel.DataAnnotations;

namespace SkyRoute.Application.Contracts;

/// <summary>
/// POST /api/search request body (architecture-plan.md Section 5, feature-flight-search.md Section 2).
/// DataAnnotations document field-level rules per AD-003; SearchRequestValidator is the
/// authoritative source of the exact validation errors returned to the client (FR-060).
/// </summary>
public sealed class SearchRequest
{
    [Required]
    public string Origin { get; set; } = string.Empty;

    [Required]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateOnly? DepartureDate { get; set; }

    [Range(1, 9)]
    public int PassengerCount { get; set; }

    [Required]
    public string CabinClass { get; set; } = string.Empty;

    [Required]
    public string TripType { get; set; } = string.Empty;
}
