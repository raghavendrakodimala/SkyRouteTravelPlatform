using System.Text.RegularExpressions;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Services;

namespace SkyRoute.Application.Tests.Services;

/// <summary>
/// Unit tests for BookingReferenceGenerator (BR-004, AD-008, DP-018). Asserts the exact
/// SKY-[INT|DOM]-[XXXXXX] format and non-degenerate randomness of the 6-character suffix.
/// </summary>
public class BookingReferenceGeneratorTests
{
    private static readonly Regex ReferenceFormat = new("^SKY-(INT|DOM)-[A-Z0-9]{6}$");

    private readonly BookingReferenceGenerator _generator = new();

    [Fact]
    public void GenerateBookingReference_International_MatchesExactFormat()
    {
        var reference = _generator.GenerateBookingReference(RouteType.International);

        Assert.Matches(ReferenceFormat, reference);
        Assert.StartsWith("SKY-INT-", reference);
        Assert.Equal(14, reference.Length);
    }

    [Fact]
    public void GenerateBookingReference_Domestic_MatchesExactFormat()
    {
        var reference = _generator.GenerateBookingReference(RouteType.Domestic);

        Assert.Matches(ReferenceFormat, reference);
        Assert.StartsWith("SKY-DOM-", reference);
        Assert.Equal(14, reference.Length);
    }

    [Fact]
    public void GenerateBookingReference_Suffix_IsUppercaseAlphanumericSixCharacters()
    {
        var reference = _generator.GenerateBookingReference(RouteType.International);
        var suffix = reference["SKY-INT-".Length..];

        Assert.Equal(6, suffix.Length);
        Assert.Matches("^[A-Z0-9]{6}$", suffix);
    }

    [Fact]
    public void GenerateBookingReference_RepeatedCalls_ProduceNonDegenerateSuffixes()
    {
        var references = Enumerable.Range(0, 50)
            .Select(_ => _generator.GenerateBookingReference(RouteType.International))
            .ToList();

        Assert.True(references.Distinct().Count() > 1, "Expected repeated generation to produce varied suffixes, not a single constant value.");
    }
}
