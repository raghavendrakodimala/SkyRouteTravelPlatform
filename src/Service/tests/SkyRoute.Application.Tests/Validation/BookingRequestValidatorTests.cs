using SkyRoute.Application.Dtos;
using SkyRoute.Application.Data;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Validation;

namespace SkyRoute.Application.Tests.Validation;

/// <summary>
/// Unit tests for BookingRequestValidator (AD-003, BL-014, feature-booking-flow.md Sections
/// 3/7). Covers ValidateStructure (step 1, route-type independent) and ValidateDocuments
/// (step 3, dependent on a server-resolved RouteType passed in directly here to simulate
/// BookingService's call).
/// </summary>
public class BookingRequestValidatorTests
{
    private readonly BookingRequestValidator _validator = new(new AirportDataService());

    private static BookingFlightRequest MakeValidFlight() => new()
    {
        Provider = "GlobalAir",
        FlightNumber = "GA101",
        Origin = "LHR",
        Destination = "JFK",
        DepartureDateTime = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
        ArrivalDateTime = new DateTime(2026, 8, 1, 17, 30, 0, DateTimeKind.Utc),
        DurationMinutes = 510,
        CabinClass = "Economy",
        BaseFare = 250.00m,
        PricePerPassenger = 287.50m,
    };

    private static PassengerRequest MakeValidPassenger(
        string fullName = "Jane Doe",
        int? age = 34,
        string email = "jane@example.com",
        string documentType = "Passport",
        string documentNumber = "AB1234C") => new()
    {
        FullName = fullName,
        Age = age,
        Email = email,
        DocumentType = documentType,
        DocumentNumber = documentNumber,
    };

    private static BookingRequest MakeValidRequest(int passengerCount = 2) => new()
    {
        Flight = MakeValidFlight(),
        PassengerCount = passengerCount,
        Passengers = Enumerable.Range(0, passengerCount).Select(_ => MakeValidPassenger()).ToList(),
    };

    // ---------------------------------------------------------------------
    // ValidateStructure
    // ---------------------------------------------------------------------

    [Fact]
    public void ValidateStructure_FullyValidRequest_ReturnsEmptyDictionary()
    {
        var errors = _validator.ValidateStructure(MakeValidRequest());

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStructure_PassengerCountMismatch_ReturnsPassengerCountMessage()
    {
        var request = MakeValidRequest();
        request.PassengerCount = 2;
        request.Passengers = new List<PassengerRequest> { MakeValidPassenger(), MakeValidPassenger(), MakeValidPassenger() };

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Passenger count must match the number of passenger records submitted." },
            errors["passengerCount"]);
    }

    [Fact]
    public void ValidateStructure_FlightMissingProvider_ReturnsFlightIncompleteMessage()
    {
        var request = MakeValidRequest();
        request.Flight.Provider = null;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "Flight details are incomplete." }, errors["flight"]);
    }

    [Fact]
    public void ValidateStructure_FlightMissingPricePerPassenger_ReturnsFlightIncompleteMessage()
    {
        var request = MakeValidRequest();
        request.Flight.PricePerPassenger = null;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "Flight details are incomplete." }, errors["flight"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("12")]
    public void ValidateStructure_PassengerFullNameInvalid_ReturnsFullNameMessage(string fullName)
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].FullName = fullName;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Full name is required, must be 2–100 characters, and must contain at least one letter." },
            errors["passengers[0].fullName"]);
    }

    [Fact]
    public void ValidateStructure_PassengerFullNameTwoCharsWithLetter_IsValid()
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].FullName = "Jo";

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("passengers[0].fullName"));
    }

    [Fact]
    public void ValidateStructure_PassengerFullNameHundredCharsValid_IsValid()
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].FullName = new string('A', 99) + "B";

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("passengers[0].fullName"));
    }

    [Theory]
    [InlineData("janeexample.com")]
    [InlineData("jane@")]
    public void ValidateStructure_PassengerEmailInvalid_ReturnsEmailMessage(string email)
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].Email = email;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "A valid email address is required." }, errors["passengers[0].email"]);
    }

    [Fact]
    public void ValidateStructure_PassengerEmailValid_IsValid()
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].Email = "jane@example.com";

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("passengers[0].email"));
    }

    // ---------------------------------------------------------------------
    // ValidateStructure — passenger age (PO age feature 2026-07-08; DEC-022: pure data
    // capture, sanity bounds 0–120 only — no business rule is bound to age)
    // ---------------------------------------------------------------------

    [Fact]
    public void ValidateStructure_AllPassengersWithValidAges_ReturnsNoAgeErrors()
    {
        var errors = _validator.ValidateStructure(MakeValidRequest());

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStructure_PassengerAgeZero_IsValid()
    {
        // 0 is the inclusive lower boundary — an infant — and is valid at ANY position,
        // including the lead (DEC-022: no lead-adult rule exists).
        var request = MakeValidRequest(2);
        request.Passengers[0].Age = 0;
        request.Passengers[1].Age = 0;

        var errors = _validator.ValidateStructure(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStructure_PassengerAgeOneHundredTwenty_IsValid()
    {
        // 120 is the inclusive upper boundary.
        var request = MakeValidRequest(1);
        request.Passengers[0].Age = 120;

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("passengers[0].age"));
    }

    [Fact]
    public void ValidateStructure_PassengerAgeMissing_ReturnsAgeMessage()
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].Age = null;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Age is required and must be a whole number between 0 and 120." },
            errors["passengers[0].age"]);
    }

    [Fact]
    public void ValidateStructure_PassengerAgeNegative_ReturnsAgeMessage()
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].Age = -1;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Age is required and must be a whole number between 0 and 120." },
            errors["passengers[0].age"]);
    }

    [Fact]
    public void ValidateStructure_PassengerAgeAboveMaximum_ReturnsAgeMessage()
    {
        var request = MakeValidRequest(2);
        request.Passengers[1].Age = 121;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Age is required and must be a whole number between 0 and 120." },
            errors["passengers[1].age"]);
    }

    [Fact]
    public void ValidateStructure_ChildPassengerAtAnyPosition_IsValid()
    {
        // No age-based business rule exists (DEC-022) — a child is valid at any index.
        var request = MakeValidRequest(2);
        request.Passengers[1].Age = 5;

        var errors = _validator.ValidateStructure(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStructure_LeadPassengerFifteen_IsValid()
    {
        // DEC-022 (PO 2026-07-08): the former AGE-LEAD-18 lead-adult rule was removed — a
        // minor lead passenger validates clean; age is pure data capture with sanity bounds.
        var request = MakeValidRequest(1);
        request.Passengers[0].Age = 15;

        var errors = _validator.ValidateStructure(request);

        Assert.Empty(errors);
    }

    // ---------------------------------------------------------------------
    // ValidateStructure — flight-fare snapshot range/allow-list checks (SEC-001)
    // ---------------------------------------------------------------------

    [Fact]
    public void ValidateStructure_PricePerPassengerZero_ReturnsPricePerPassengerMessage()
    {
        var request = MakeValidRequest();
        request.Flight.PricePerPassenger = 0m;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Price per passenger must be greater than zero." },
            errors["flight.pricePerPassenger"]);
    }

    [Fact]
    public void ValidateStructure_PricePerPassengerNegative_ReturnsPricePerPassengerMessage()
    {
        var request = MakeValidRequest();
        request.Flight.PricePerPassenger = -50.00m;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Price per passenger must be greater than zero." },
            errors["flight.pricePerPassenger"]);
    }

    [Fact]
    public void ValidateStructure_BaseFareZero_ReturnsBaseFareMessage()
    {
        var request = MakeValidRequest();
        request.Flight.BaseFare = 0m;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Base fare must be greater than zero." },
            errors["flight.baseFare"]);
    }

    [Fact]
    public void ValidateStructure_BaseFareNegative_ReturnsBaseFareMessage()
    {
        var request = MakeValidRequest();
        request.Flight.BaseFare = -10.00m;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Base fare must be greater than zero." },
            errors["flight.baseFare"]);
    }

    [Fact]
    public void ValidateStructure_BaseFareNull_DoesNotReturnBaseFareMessage()
    {
        // BaseFare is not a required field on the snapshot (IsFlightSnapshotComplete does not
        // require it); the >0 check should only apply when a value is actually present.
        var request = MakeValidRequest();
        request.Flight.BaseFare = null;

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("flight.baseFare"));
    }

    [Theory]
    [InlineData("Coach")]
    [InlineData("economy")]
    [InlineData("first class")]
    [InlineData("Business Plus")]
    public void ValidateStructure_CabinClassNotInAllowList_ReturnsCabinClassMessage(string cabinClass)
    {
        var request = MakeValidRequest();
        request.Flight.CabinClass = cabinClass;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Cabin class must be one of: Economy, Business, First Class." },
            errors["flight.cabinClass"]);
    }

    [Theory]
    [InlineData("Economy")]
    [InlineData("Business")]
    [InlineData("First Class")]
    public void ValidateStructure_CabinClassInAllowList_IsValid(string cabinClass)
    {
        var request = MakeValidRequest();
        request.Flight.CabinClass = cabinClass;

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("flight.cabinClass"));
    }

    [Fact]
    public void ValidateStructure_ValidPriceAndCabinClass_ReturnsEmptyDictionary()
    {
        var request = MakeValidRequest();
        request.Flight.PricePerPassenger = 199.99m;
        request.Flight.BaseFare = 150.00m;
        request.Flight.CabinClass = "Business";

        var errors = _validator.ValidateStructure(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStructure_NullFlightAndNullPassengers_ReturnsFlightError_DoesNotThrow()
    {
        // Defensive null-guard case, HO-012A decision 7: a caller submits "flight": null and
        // "passengers": null directly (bypassing the property initializer defaults).
        var request = new BookingRequest
        {
            Flight = null!,
            Passengers = null!,
            PassengerCount = 0,
        };

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "Flight details are incomplete." }, errors["flight"]);
    }

    // ---------------------------------------------------------------------
    // ValidateStructure — passenger-count upper bound (SEC-002)
    // ---------------------------------------------------------------------

    [Fact]
    public void ValidateStructure_PassengerCountZero_ReturnsPassengerCountRangeMessage()
    {
        var request = new BookingRequest
        {
            Flight = MakeValidFlight(),
            PassengerCount = 0,
            Passengers = new List<PassengerRequest>(),
        };

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Passenger count must be a whole number between 1 and 9." },
            errors["passengerCount"]);
    }

    [Fact]
    public void ValidateStructure_PassengerCountAboveNine_ReturnsPassengerCountRangeMessage()
    {
        var request = MakeValidRequest(10);

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(
            new[] { "Passenger count must be a whole number between 1 and 9." },
            errors["passengerCount"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void ValidateStructure_PassengerCountWithinRange_DoesNotReturnPassengerCountRangeMessage(int passengerCount)
    {
        var request = MakeValidRequest(passengerCount);

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("passengerCount"));
    }

    // ---------------------------------------------------------------------
    // ValidateStructure — email length upper bound (SEC-004)
    // ---------------------------------------------------------------------

    [Fact]
    public void ValidateStructure_PassengerEmailOverMaxLength_ReturnsEmailMessage()
    {
        var request = MakeValidRequest(1);
        // Local part padded so the overall address is 255 characters — one over the RFC 5321
        // practical maximum of 254 enforced by DocumentPatterns.EmailPattern's lookahead bound.
        var oversizedLocalPart = new string('a', 255 - "@example.com".Length);
        request.Passengers[0].Email = oversizedLocalPart + "@example.com";

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "A valid email address is required." }, errors["passengers[0].email"]);
    }

    [Fact]
    public void ValidateStructure_PassengerEmailAtMaxLength_IsValid()
    {
        var request = MakeValidRequest(1);
        // Exactly 254 characters total, the boundary the pattern's lookahead permits.
        var localPart = new string('a', 254 - "@example.com".Length);
        request.Passengers[0].Email = localPart + "@example.com";

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("passengers[0].email"));
    }

    // ---------------------------------------------------------------------
    // ValidateStructure — flight-snapshot sanity: airport codes, chronology, past
    // departure (AUD-029/026), and the timezone-generous date boundary (AUD-031)
    // ---------------------------------------------------------------------

    [Fact]
    public void ValidateStructure_UnknownOriginAirportCode_ReturnsOriginNotRecognizedMessage()
    {
        var request = MakeValidRequest(1);
        request.Flight.Origin = "ZZZ";

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "Origin airport code is not recognized." }, errors["flight.origin"]);
    }

    [Fact]
    public void ValidateStructure_UnknownDestinationAirportCode_ReturnsDestinationNotRecognizedMessage()
    {
        var request = MakeValidRequest(1);
        request.Flight.Destination = "QQQ";

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "Destination airport code is not recognized." }, errors["flight.destination"]);
    }

    [Fact]
    public void ValidateStructure_ArrivalBeforeDeparture_ReturnsArrivalMessage()
    {
        var request = MakeValidRequest(1);
        request.Flight.DepartureDateTime = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        request.Flight.ArrivalDateTime = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "Arrival must be after departure." }, errors["flight.arrivalDateTime"]);
    }

    [Fact]
    public void ValidateStructure_ArrivalEqualsDeparture_ReturnsArrivalMessage()
    {
        var request = MakeValidRequest(1);
        var instant = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        request.Flight.DepartureDateTime = instant;
        request.Flight.ArrivalDateTime = instant;

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "Arrival must be after departure." }, errors["flight.arrivalDateTime"]);
    }

    [Fact]
    public void ValidateStructure_PastDeparture_ReturnsDepartureInPastMessage()
    {
        // Two days before UTC today is unambiguously past for every timezone (AUD-026/029).
        var request = MakeValidRequest(1);
        var past = DateTime.UtcNow.AddDays(-2);
        request.Flight.DepartureDateTime = past;
        request.Flight.ArrivalDateTime = past.AddHours(2);

        var errors = _validator.ValidateStructure(request);

        Assert.Equal(new[] { "Departure cannot be in the past." }, errors["flight.departureDateTime"]);
    }

    [Fact]
    public void ValidateStructure_DepartureYesterdayUtc_IsAcceptedWithinTimezoneGrace()
    {
        // AUD-031: a departure dated UTC-yesterday is a legitimate "today" for a negative-offset
        // client, so it must NOT be flagged as past (timezone grace band, DepartureDateRules).
        var request = MakeValidRequest(1);
        var yesterday = DateTime.UtcNow.AddDays(-1);
        request.Flight.DepartureDateTime = yesterday;
        request.Flight.ArrivalDateTime = yesterday.AddHours(2);

        var errors = _validator.ValidateStructure(request);

        Assert.False(errors.ContainsKey("flight.departureDateTime"));
    }

    // ---------------------------------------------------------------------
    // ValidateStructure — passengers-array bound (AUD-034): the oversized array is
    // rejected BEFORE the per-element loop runs (no per-passenger errors are produced).
    // ---------------------------------------------------------------------

    [Fact]
    public void ValidateStructure_OversizedPassengersArray_RejectsWithoutWalkingTheArray()
    {
        var request = MakeValidRequest();
        request.PassengerCount = 5;
        // 50 empty passenger objects: if the per-element loop ran it would add
        // passengers[0..49].fullName/age/email errors. It must be short-circuited instead.
        request.Passengers = Enumerable.Range(0, 50).Select(_ => new PassengerRequest()).ToList();

        var errors = _validator.ValidateStructure(request);

        Assert.True(errors.ContainsKey("passengers"));
        Assert.Equal(new[] { "A booking may include at most 9 passengers." }, errors["passengers"]);
        Assert.DoesNotContain(errors.Keys, k => k.StartsWith("passengers[", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------
    // ValidateDocuments
    // ---------------------------------------------------------------------

    [Fact]
    public void ValidateDocuments_InternationalValidPassport_IsValid()
    {
        // Worked example, feature-booking-flow.md Section 2.3.1.
        var request = MakeValidRequest(1);
        request.Passengers[0].DocumentType = "Passport";
        request.Passengers[0].DocumentNumber = "AB1234C";

        var errors = _validator.ValidateDocuments(request, RouteType.International);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("ab1234c")] // lowercase
    [InlineData("AB12")] // below 6-char minimum
    [InlineData("AB12345678")] // above 9-char maximum (10 chars)
    public void ValidateDocuments_InternationalInvalidPassportNumber_ReturnsPassportMessage(string documentNumber)
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].DocumentType = "Passport";
        request.Passengers[0].DocumentNumber = documentNumber;

        var errors = _validator.ValidateDocuments(request, RouteType.International);

        Assert.Equal(
            new[] { "Passport number must be 6–9 uppercase letters and digits, with no spaces." },
            errors["passengers[0].documentNumber"]);
    }

    [Fact]
    public void ValidateDocuments_DomesticValidNationalId_IsValid()
    {
        // Worked example, feature-booking-flow.md Section 2.3.1.
        var request = MakeValidRequest(1);
        request.Passengers[0].DocumentType = "National ID";
        request.Passengers[0].DocumentNumber = "AB-1234";

        var errors = _validator.ValidateDocuments(request, RouteType.Domestic);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("AB12")] // below 5-char minimum
    public void ValidateDocuments_DomesticInvalidNationalIdBelowMinimum_ReturnsNationalIdMessage(string documentNumber)
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].DocumentType = "National ID";
        request.Passengers[0].DocumentNumber = documentNumber;

        var errors = _validator.ValidateDocuments(request, RouteType.Domestic);

        Assert.Equal(
            new[] { "National ID must be 5–20 letters, digits, or hyphens, with no spaces." },
            errors["passengers[0].documentNumber"]);
    }

    [Fact]
    public void ValidateDocuments_DomesticInvalidNationalIdAboveMaximum_ReturnsNationalIdMessage()
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].DocumentType = "National ID";
        request.Passengers[0].DocumentNumber = new string('A', 21);

        var errors = _validator.ValidateDocuments(request, RouteType.Domestic);

        Assert.Equal(
            new[] { "National ID must be 5–20 letters, digits, or hyphens, with no spaces." },
            errors["passengers[0].documentNumber"]);
    }

    [Fact]
    public void ValidateDocuments_InternationalRouteWithNationalIdDocumentType_ReturnsDocumentTypeMismatchMessage()
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].DocumentType = "National ID";
        request.Passengers[0].DocumentNumber = "AB-1234";

        var errors = _validator.ValidateDocuments(request, RouteType.International);

        Assert.Equal(
            new[] { "Document type does not match the route for this booking." },
            errors["passengers[0].documentType"]);
    }

    [Fact]
    public void ValidateDocuments_DomesticRouteWithPassportDocumentType_ReturnsDocumentTypeMismatchMessage()
    {
        var request = MakeValidRequest(1);
        request.Passengers[0].DocumentType = "Passport";
        request.Passengers[0].DocumentNumber = "AB1234C";

        var errors = _validator.ValidateDocuments(request, RouteType.Domestic);

        Assert.Equal(
            new[] { "Document type does not match the route for this booking." },
            errors["passengers[0].documentType"]);
    }
}
