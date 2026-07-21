using System.Net;
using Reqnroll;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7003DeleteTrip
{
    private readonly int tripId = 11;
    private readonly AcceptanceTestFixture _fixture;

    public U7003DeleteTrip(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Given("I have a trip")]
    async public Task GivenIHaveATrip()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Trips.Add(new Models.Entities.Trip
        {
            Id = tripId,
            Title = "title ya'll",
            Description = "description",
            StartDate = DateTime.Parse("2022-01-01"),
            EndDate = DateTime.Parse("2022-01-07"),
            UserId = 1
        });
        
        await context.SaveChangesAsync();
    }

    [When("I delete the trip")]
    async public Task WhenIDeleteTheTrip()
    {
        await _fixture.HttpClient.DeleteAsync($"api/trip/{tripId}");
    }

    [Then("the trip no longer exists in the database")]
    async public Task ThenTheTripNoLongerExistsInTheDatabase()
    {
        var getCall = await _fixture.HttpClient.GetAsync($"api/trip/{tripId}");

        getCall.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
