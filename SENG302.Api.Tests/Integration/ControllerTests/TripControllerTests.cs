using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Entities;
using Shouldly;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class TripControllerTests : BaseIntegrationTestFixture
{
    public TripControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory) { }

    /// <summary>
    /// Creates and adds to the database a test user for these tests
    /// </summary>
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

    /// <summary>
    /// Some trip data that is used by several tests
    /// </summary>
    private readonly NewTripRequest tripTestData = new()
    {
        Title = "Test Trip",
        Description = "Test",
        StartDate = new DateOnly(2025, 11, 21),
        EndDate = new DateOnly(2025, 12, 21)
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
            StartDate = new DateOnly(2025, 11, 21),
            EndDate = new DateOnly(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Trip title is required and must be between 3 and 128 characters long");
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
            StartDate = new DateOnly(2025, 11, 21),
            EndDate = new DateOnly(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Trip title cannot contain characters other than letters, spaces, hyphens, apostrophes, or numbers");
    }

    [Fact]
    public async Task CreateTrip_EndDateBeforeStartDate_ReturnBadRequest()
    {
        await AddTestUser();

        var data = new NewTripRequest
        {
            Title = "Trip!",
            Description = "Test",
            StartDate = new DateOnly(2025, 11, 21),
            EndDate = new DateOnly(2025, 10, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("End date must not be before the start date");
    }

    [Fact]
    public async Task CreateTrip_DatesAreNull_ReturnBadRequest()
    {
        await AddTestUser();

        var data = new NewTripRequest
        {
            Title = "Trip!",
            Description = "Test",
            StartDate = null,
            EndDate = null
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Invalid date, date must be formatted ");
    }


    [Fact]
    public async Task FetchTripsByUser_SuccessfulFetch_ReturnsTrips()
    {
        await AddTestUser();
        await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);

        // Fetch trips for the user
        var response = await HttpClient.GetAsync("/api/trip");
        response.StatusCode.ShouldBe(HttpStatusCode.OK); // Ensure fetching trips was successful
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

    [Fact]
    public async Task UpdateTrip_ExistingTrip_ReturnsOkAndPersistsChanges()
    {
        await AddTestUser();

        var createResponse =
            await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);

        var createdTrip =
            await createResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(createdTrip);

        var newStartDate = new DateOnly(2026, 8, 1);
        var newEndDate = new DateOnly(2026, 8, 10);

        var request = new UpdateTripRequest
        {
            Id = createdTrip.Id,
            Title = "Updated trip",
            Description = "Updated description",
            StartDate = newStartDate,
            EndDate = newEndDate
        };

        var updateResponse = await HttpClient.PutAsJsonAsync(
            $"/api/trip/{createdTrip.Id}",
            request
        );

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var returnedTrip =
            await updateResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(returnedTrip);
        returnedTrip.Title.ShouldBe("Updated trip");
        returnedTrip.Description.ShouldBe("Updated description");
        returnedTrip.StartDate.ShouldBe(newStartDate);
        returnedTrip.EndDate.ShouldBe(newEndDate);

        var getResponse =
            await HttpClient.GetAsync($"/api/trip/{createdTrip.Id}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var savedTrip =
            await getResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(savedTrip);
        savedTrip.Title.ShouldBe("Updated trip");
        savedTrip.Description.ShouldBe("Updated description");
        savedTrip.StartDate.ShouldBe(newStartDate);
        savedTrip.EndDate.ShouldBe(newEndDate);
    }

    [Fact]
    public async Task UpdateTrip_PartialRequest_PreservesUnspecifiedFields()
    {
        await AddTestUser();

        var createResponse =
            await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);

        var createdTrip =
            await createResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(createdTrip);

        var request = new UpdateTripRequest
        {
            Id = createdTrip.Id,
            Title = "Updated title",
            StartDate = createdTrip.StartDate,
            EndDate = createdTrip.EndDate
        };

        var updateResponse = await HttpClient.PutAsJsonAsync(
            $"/api/trip/{createdTrip.Id}",
            request
        );

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updatedTrip =
            await updateResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(updatedTrip);
        updatedTrip.Title.ShouldBe("Updated title");
        updatedTrip.Description.ShouldBe(createdTrip.Description);
        updatedTrip.StartDate.ShouldBe(createdTrip.StartDate);
        updatedTrip.EndDate.ShouldBe(createdTrip.EndDate);
        updatedTrip.UserId.ShouldBe(createdTrip.UserId);
    }
}