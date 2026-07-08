using SkyRoute.Application.Contracts;
using SkyRoute.Application.Data;
using SkyRoute.Application.Validation;

namespace SkyRoute.Application.Tests.Validation;

/// <summary>
/// Unit tests for SearchRequestValidator (AD-003, BL-010, feature-flight-search.md Section
/// 4.1). Field keys and exact message strings asserted here are frozen spec text, not
/// paraphrases. Also asserts the validator does not short-circuit (FR-063).
/// </summary>
public class SearchRequestValidatorTests
{
    private readonly SearchRequestValidator _validator = new(new AirportDataService());

    private static SearchRequest MakeValidRequest() => new()
    {
        Origin = "LHR",
        Destination = "JFK",
        DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
        PassengerCount = 2,
        CabinClass = "Economy",
        TripType = "OneWay",
    };

    [Fact]
    public void Validate_FullyValidRequest_ReturnsEmptyDictionary()
    {
        var errors = _validator.Validate(MakeValidRequest());

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_OriginMissing_ReturnsRequiredMessage(string? origin)
    {
        var request = MakeValidRequest();
        request.Origin = origin!;

        var errors = _validator.Validate(request);

        Assert.Equal(
            new[] { "Origin airport code is required and must be a valid 3-letter airport code." },
            errors["origin"]);
    }

    [Theory]
    [InlineData("lhr")]
    [InlineData("LH")]
    public void Validate_OriginMalformed_ReturnsRequiredMessage(string origin)
    {
        var request = MakeValidRequest();
        request.Origin = origin;

        var errors = _validator.Validate(request);

        Assert.Equal(
            new[] { "Origin airport code is required and must be a valid 3-letter airport code." },
            errors["origin"]);
    }

    [Fact]
    public void Validate_OriginWellFormedButUnknown_ReturnsNotRecognizedMessage()
    {
        var request = MakeValidRequest();
        request.Origin = "ZZZ";

        var errors = _validator.Validate(request);

        Assert.Equal(new[] { "Origin airport code is not recognized." }, errors["origin"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_DestinationMissing_ReturnsRequiredMessage(string? destination)
    {
        var request = MakeValidRequest();
        request.Destination = destination!;

        var errors = _validator.Validate(request);

        Assert.Equal(
            new[] { "Destination airport code is required and must be a valid 3-letter airport code." },
            errors["destination"]);
    }

    [Fact]
    public void Validate_DestinationUnknown_ReturnsNotRecognizedMessage()
    {
        var request = MakeValidRequest();
        request.Destination = "ZZZ";

        var errors = _validator.Validate(request);

        Assert.Equal(new[] { "Destination airport code is not recognized." }, errors["destination"]);
    }

    [Fact]
    public void Validate_OriginEqualsDestination_ReturnsMustBeDifferentMessage()
    {
        var request = MakeValidRequest();
        request.Origin = "LHR";
        request.Destination = "LHR";

        var errors = _validator.Validate(request);

        Assert.Equal(new[] { "Origin and destination airports must be different." }, errors["destination"]);
    }

    [Fact]
    public void Validate_DepartureDateNull_ReturnsRequiredMessage()
    {
        var request = MakeValidRequest();
        request.DepartureDate = null;

        var errors = _validator.Validate(request);

        Assert.Equal(new[] { "Departure date is required and must be a valid date." }, errors["departureDate"]);
    }

    [Fact]
    public void Validate_DepartureDateTwoDaysPast_ReturnsCannotBeInPastMessage()
    {
        // AUD-026/031: the past-date boundary is now the timezone-generous UTC-1 (see
        // DepartureDateRules), so a date must be at least two days behind UTC to be unambiguously
        // in the past for every timezone and rejected. (Was AddDays(-1) — that value is now
        // accepted, see Validate_DepartureDateYesterdayUtc_IsAcceptedWithinTimezoneGrace.)
        var request = MakeValidRequest();
        request.DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);

        var errors = _validator.Validate(request);

        Assert.Equal(new[] { "Departure date cannot be in the past." }, errors["departureDate"]);
    }

    [Fact]
    public void Validate_DepartureDateYesterdayUtc_IsAcceptedWithinTimezoneGrace()
    {
        // AUD-031 regression: a negative-offset user's legitimate LOCAL "today" can read as
        // yesterday in UTC once UTC has rolled over. Anchoring the boundary at UTC-1 means such a
        // same-day search is accepted rather than wrongly rejected as past.
        var request = MakeValidRequest();
        request.DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var errors = _validator.Validate(request);

        Assert.False(errors.ContainsKey("departureDate"));
    }

    [Fact]
    public void Validate_DepartureDateToday_IsValid()
    {
        var request = MakeValidRequest();
        request.DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var errors = _validator.Validate(request);

        Assert.False(errors.ContainsKey("departureDate"));
    }

    [Fact]
    public void Validate_DepartureDateFuture_IsValid()
    {
        var request = MakeValidRequest();
        request.DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        var errors = _validator.Validate(request);

        Assert.False(errors.ContainsKey("departureDate"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Validate_PassengerCountOutOfRange_ReturnsRangeMessage(int passengerCount)
    {
        var request = MakeValidRequest();
        request.PassengerCount = passengerCount;

        var errors = _validator.Validate(request);

        Assert.Equal(
            new[] { "Passenger count must be a whole number between 1 and 9." },
            errors["passengerCount"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void Validate_PassengerCountBoundaryValid_IsValid(int passengerCount)
    {
        var request = MakeValidRequest();
        request.PassengerCount = passengerCount;

        var errors = _validator.Validate(request);

        Assert.False(errors.ContainsKey("passengerCount"));
    }

    [Theory]
    [InlineData("PremiumEconomy")]
    [InlineData("")]
    public void Validate_CabinClassInvalid_ReturnsCabinClassMessage(string cabinClass)
    {
        var request = MakeValidRequest();
        request.CabinClass = cabinClass;

        var errors = _validator.Validate(request);

        Assert.Equal(
            new[] { "Cabin class must be one of: Economy, Business, First Class." },
            errors["cabinClass"]);
    }

    [Theory]
    [InlineData("Economy")]
    [InlineData("Business")]
    [InlineData("First Class")]
    public void Validate_CabinClassValid_IsValid(string cabinClass)
    {
        var request = MakeValidRequest();
        request.CabinClass = cabinClass;

        var errors = _validator.Validate(request);

        Assert.False(errors.ContainsKey("cabinClass"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("RoundTrip")]
    public void Validate_TripTypeInvalid_ReturnsTripTypeMessage(string tripType)
    {
        var request = MakeValidRequest();
        request.TripType = tripType;

        var errors = _validator.Validate(request);

        Assert.Equal(new[] { "Trip type must be 'OneWay'." }, errors["tripType"]);
    }

    [Fact]
    public void Validate_TripTypeOneWay_IsValid()
    {
        var request = MakeValidRequest();
        request.TripType = "OneWay";

        var errors = _validator.Validate(request);

        Assert.False(errors.ContainsKey("tripType"));
    }

    [Fact]
    public void Validate_MultipleSimultaneousFailures_DoesNotShortCircuit()
    {
        var request = MakeValidRequest();
        // AUD-026/031: two days past is unambiguously in the past (UTC-1 grace boundary).
        request.DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        request.PassengerCount = 10;

        var errors = _validator.Validate(request);

        Assert.True(errors.ContainsKey("departureDate"));
        Assert.True(errors.ContainsKey("passengerCount"));
    }
}
