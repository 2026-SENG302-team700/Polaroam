using System.Net.Http.Json;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;
using System.Net;
using System.Runtime.InteropServices.JavaScript;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7001CreateTrip
{

    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;
    private string _title;
    private string _description;
    private DateOnly _startDate;
    private DateOnly _endDate;

    public U7001CreateTrip(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
        _title = "";
        _description = "";
        _startDate = new DateOnly();
        _endDate = new DateOnly();

    }

    [Given(
        @"I am on the create trip form page And I enter a valid title, an optional valid description, a valid date range")]
    public async Task ValidTripAttributes()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new Models.Entities.User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        _title = "Test trip";
        _description = "Test description";
        _startDate = new DateOnly(2022, 1, 1);
        _endDate = new DateOnly(2022, 2, 1);

        await context.SaveChangesAsync();
    }

    [When(@"I click 'Create Trip'")]
    public async Task CreateTrip()
    {
        NewTripRequest tripRequest = new NewTripRequest
        {
            Title = _title,
            Description = _description,
            StartDate = _startDate,
            EndDate = _endDate,
        };

        _lastResponse = await _fixture.HttpClient.PostAsJsonAsync("api/trip", tripRequest);
    }

    [Then(@"the trip is persisted")]
    public async Task checkTripIsPersisted()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trip = _lastResponse.Content.ReadFromJsonAsync<Trip>();

        var retrievedTrip = await _fixture.HttpClient.GetFromJsonAsync<Trip>($"api/trip/{trip.Id}");

        retrievedTrip.Id.ShouldBe(trip.Id);
    }

    [Given(
        @"I am on the create trip form page And I select an end date that is before the selected start date")]
    public async Task InvalidTripEndDateBeforeStartDate()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new Models.Entities.User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        _title = "Test trip";
        _description = "Test description";
        _startDate = new DateOnly(2022, 1, 1);
        _endDate = new DateOnly(2021, 2, 1);

        await context.SaveChangesAsync();
    }

    [Then(@"the trip is not created, And I see an error message 'End date must not be before the start date' preserved and displayed in the trip description")]
    public async Task checkDateErrorStartDateBeforeEndDate()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await _lastResponse.Content.ReadAsStringAsync();
        error.ShouldContain("End date must not be before the start date");
    }

    // AC 3
    [Given(
        @"I am on the create trip form page And I enter a short title")]
    public async Task EnterShortTitle()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new Models.Entities.User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        _title = "T";

        await context.SaveChangesAsync();
    }

    [Then(@"I see the error message 'Trip title is required and must be between 3 and 128 characters long'")]
    public async Task checkTitleErrorTooShort()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await _lastResponse.Content.ReadAsStringAsync();
        error.ShouldContain("Trip title is required and must be between 3 and 128 characters long");
    }

    // AC 7
    [Given(@"I am on the create trip form page And I enter a long description")]
    public async Task EnterLongDescriptionTitle()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new Models.Entities.User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        _description = new string('a', 2049);

        await context.SaveChangesAsync();
    }

    [Then(@"I see the error message 'Description must be 2048 characters or less'")]
    public async Task checkDescriptionErrorTooLong()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await _lastResponse.Content.ReadAsStringAsync();
        error.ShouldContain("Description must be 2048 characters or less");
    }
}