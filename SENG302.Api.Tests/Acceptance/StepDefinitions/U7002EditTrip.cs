using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7002EditTrip
{
    private readonly AcceptanceTestFixture _fixture;
    private Trip? _originalTrip;
    private HttpResponseMessage? _lastResponse;

    public U7002EditTrip(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Given("I have an existing trip")]
    public async Task GivenIHaveAnExistingTrip()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        context.Trips.Add(new Trip
        {
            Title = "Original trip",
            Description = "Original description",
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 10),
            UserId = 1
        });

        await context.SaveChangesAsync();

        _originalTrip = await context.Trips.SingleAsync();
    }

    [Given("I am editing that trip")]
    public void GivenIAmEditingThatTrip()
    {
        _originalTrip.ShouldNotBeNull();
    }

    [When("I update the trip with valid information")]
    public async Task WhenIUpdateTheTripWithValidInformation()
    {
        _originalTrip.ShouldNotBeNull();

        var request = new UpdateTripRequest
        {
            Id = _originalTrip.Id,
            Title = "Updated trip",
            Description = "Updated description",
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 10)
        };

        _lastResponse = await _fixture.HttpClient.PutAsJsonAsync(
            $"api/trip/{_originalTrip.Id}",
            request
        );
    }

    [Then("the changes to the trip are persisted")]
    public async Task ThenTheChangesToTheTripArePersisted()
    {
        _originalTrip.ShouldNotBeNull();
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var savedTrip = await _fixture.HttpClient.GetFromJsonAsync<Trip>(
            $"api/trip/{_originalTrip.Id}"
        );

        savedTrip.ShouldNotBeNull();
        savedTrip.Title.ShouldBe("Updated trip");
        savedTrip.Description.ShouldBe("Updated description");
        savedTrip.StartDate.ShouldBe(new DateTime(2026, 8, 1));
        savedTrip.EndDate.ShouldBe(new DateTime(2026, 8, 10));
    }

    [Then("I am transported back to the trip page")]
    public void ThenIAmTransportedBackToTheTripPage()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}