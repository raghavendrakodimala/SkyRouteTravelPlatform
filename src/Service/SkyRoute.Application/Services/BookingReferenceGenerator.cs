using System.Security.Cryptography;
using SkyRoute.Application.Domain;

namespace SkyRoute.Application.Services;

/// <summary>
/// Standalone, dependency-free booking reference generator (AD-008, DP-018). Independently
/// unit-testable without constructing BookingService's dependency graph. Uses
/// RandomNumberGenerator (cryptographically secure), never System.Random (BR-004).
/// </summary>
public sealed class BookingReferenceGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int SuffixLength = 6;

    /// <summary>
    /// Produces a reference in the exact format SKY-[INT|DOM]-[XXXXXX] (BR-004): 14
    /// characters total, 6-character uppercase-alphanumeric suffix drawn from a
    /// cryptographically secure, unbiased random source.
    /// </summary>
    public string GenerateBookingReference(RouteType routeType)
    {
        var type = routeType.IsInternational ? "INT" : "DOM";
        var suffix = GenerateRandomSuffix(SuffixLength);
        return $"SKY-{type}-{suffix}";
    }

    private static string GenerateRandomSuffix(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(0, Alphabet.Length)];
        }

        return new string(chars);
    }
}
