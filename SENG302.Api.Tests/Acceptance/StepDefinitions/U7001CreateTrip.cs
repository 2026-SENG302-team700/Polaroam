using System.Net.Http.Json;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7001CreateTrip
{
    
    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;

    public U7001CreateTrip(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
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
        
        await context.SaveChangesAsync();
    }

    [When(@"I click 'Create Trip'")]
    public async Task CreateTrip()
    {
        NewTripRequest tripRequest = new NewTripRequest
        {
            Title = "Test trip", Description = "description", StartDate = DateTime.Parse("2022-01-01"), EndDate = DateTime.Parse("2022-01-07"),
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
}