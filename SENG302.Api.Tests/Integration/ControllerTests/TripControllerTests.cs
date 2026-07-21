using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Entities;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class TripControllerTests : BaseIntegrationTestFixture
{
    public TripControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory) { }

    private async Task AddTestUser()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();
    }

    private readonly NewTripRequest tripTestData = new()
    {
        Title = "Test Trip",
        Description = "Test",
        StartDate = new DateTime(2025, 11, 21), 
        EndDate = new DateTime(2025, 12, 21)
    };

    [Fact]
    public async Task CreateTrip_SuccessfulCreation_ReturnOk()
    {
        await AddTestUser();

        var message = await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTrip_ShortTitle_ReturnBadRequest()
    {
        await AddTestUser();

        var data = new NewTripRequest
        {
            Title = "T",
            Description = "Test",
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Trip title must be between 3 and 128 characters long");
    }
    

    [Fact]
    public async Task CreateTrip_NonExistentUser_ReturnBadRequest()
    {
        // Test automatically gets userEmail as test@example.com so we don't add the user
        // to the db for this test
        var response = await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("User with the provided id does not exist.");
    }

    [Fact]
    public async Task CreateTrip_InvalidCharactersInName_ReturnBadRequest()
    {
        await AddTestUser();

        var data = new NewTripRequest
        {
            Title = "Trip!",
            Description = "Test",
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Trip title cannot contain characters other than letters, spaces, hyphens, apostrophes, or numbers");
    }
    
        
    [Fact]
    public async Task FetchTripsByUser_SuccessfulFetch_ReturnsTaskLists()
    {
        await AddTestUser();
        await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);

        // Fetch task lists for the user
        var response = await HttpClient.GetAsync("/api/trip");
        response.StatusCode.ShouldBe(HttpStatusCode.OK); // Ensure fetching task lists was successful
        Trip[] trips = await response.Content.ReadFromJsonAsync<Trip[]>() ?? Array.Empty<Trip>();
        trips.Length.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteTrip_TripExists_Success()
    {
        await AddTestUser();
        var postResponse = await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);
        var trip = await postResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(trip);

        var deleteResponse = await HttpClient.DeleteAsync($"/api/trip/{trip.Id}");
        var responseMessage = await deleteResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        responseMessage.ShouldBe("");
    }

    [Fact]
    public async Task DeleteTrip_TripNotExist_BadRequest()
    {
        var deleteResponse = await HttpClient.DeleteAsync($"/api/trip/{0}");
        var responseMessage = await deleteResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
        responseMessage.ShouldBe("Trip does not exist");
    }

    [Fact]
    public async Task DeleteTrip_DifferentUser_Unauthorized()
    {
        var tripId = 10;

        await using var context = DbContextFactory.CreateDbContext();
        context.Trips.Add(new Trip
        {
            Id = tripId,
            Description = tripTestData.Description,
            Title = tripTestData.Title,
            EndDate = tripTestData.EndDate,
            StartDate = tripTestData.StartDate,
            UserId = 100
        });
        await context.SaveChangesAsync();

        var deleteResponse = await HttpClient.DeleteAsync($"/api/trip/{tripId}");

        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }
}