using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Entities;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class TaskListControllerTests : BaseIntegrationTestFixture
{
    public TaskListControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory) { }

    [Fact]
    public async Task CreateTrip_SuccessfulCreation_ReturnOk()
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

        var data = new NewTripRequest
        {
            UserId = 1,
            Title = "Test Trip",
            Description = "Test",
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        };
        var message = await HttpClient.PostAsJsonAsync("/api/trip", data);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTrip_ShortTitle_ReturnBadRequest()
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

        var data = new NewTripRequest
        {
            UserId = 1,
            Title = "T",
            Description = "Test",
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Trip name is required and must be between 3 and 128 characters long");
    }

    [Fact]
    public async Task CreateTrip_NonExistentUser_ReturnBadRequest()
    {
        // Test automatically gets userEamil as test@example.com so we dont add this
        // to the db for this test
        var data = new NewTripRequest
        {
            UserId = 1,
            Title = "Valid Trip Data",
            Description = "Test",
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("User with the provided id does not exist.");
    }

    [Fact]
    public async Task CreateTrip_InvalidCharactersInName_ReturnBadRequest()
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

        var data = new NewTripRequest
        {
            UserId = 1,
            Title = "Trip!",
            Description = "Test",
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Trip name cannot contain characters other than letters, spaces, hyphens, apostrophes, or numbers");
    }
    
        
    [Fact]
    public async Task FetchTripsByUser_SuccessfulFetch_ReturnsTaskLists()
    {
        // Create DB
        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        // Create task list for user
        var data = new NewTripRequest
        {
            UserId = 1,
            Title = "Test Trip",
            Description = "Test",
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);

        // Fetch task lists for the user
        response = await HttpClient.GetAsync("/api/trip");
        response.StatusCode.ShouldBe(HttpStatusCode.OK); // Ensure fetching task lists was successful
        Trip[] trips = await response.Content.ReadFromJsonAsync<Trip[]>() ?? Array.Empty<Trip>();
        trips.Length.ShouldBe(1);
    }
}