using SkyRoute.Application.Contracts;
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
    private readonly BookingRequestValidator _validator = new();

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
        string email = "jane@example.com",
        string documentType = "Passport",
        string documentNumber = "AB1234C") => new()
    {
        FullName = fullName,
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
