using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class ScrapboardControllerTests :BaseIntegrationTestFixture
{

    public ScrapboardControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory){}

    private async Task SetupUserAndTrip(int userId, int tripId)
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = userId,
            Email = $"test{userId}@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Users.Add(new User
        {
            Id = userId+1,
            Email = $"test{userId+1}@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip
        {
            Id = tripId,
            Title = "Test List",
            UserId = userId,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Albums.Add(new Album
        {
            TripId = tripId,
            PhotoIds = [],
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetScrapboard_ValidData_Ok()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();
        
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1,
        });
        await context.SaveChangesAsync();

        var message = await HttpClient.GetAsync("/api/scrapboard/1");
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetScrapboard_BoardDoesNotExist_NotFound()
    {
        await SetupUserAndTrip(1,1);
        var message = await HttpClient.GetAsync("/api/scrapboard/1");
        message.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetScrapboard_TripDoesNotExist_BadRequest()
    {
        await SetupUserAndTrip(1,1);
        var message = await HttpClient.GetAsync("/api/scrapboard/4");
        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetScrapboard_NotPermittedToAccessBoard_Unauthorized()
    {
        int userId = 1;
        int tripId = 1;
        await SetupUserAndTrip(userId,tripId);

        await using var context = DbContextFactory.CreateDbContext();
        
        context.Trips.Add(new Trip
        {
            Id = tripId+1,
            Title = "Test List",
            UserId = userId+1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = tripId+1,
        });
        await context.SaveChangesAsync();

        var message = await HttpClient.GetAsync("/api/scrapboard/2");
        message.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateScrapboard_ValidData_Ok()
    {
        await SetupUserAndTrip(1,1);
        var newBoard = new NewScrapboardRequest()
        {
            TripId = 1,
        };
        var message = await HttpClient.PostAsJsonAsync("/api/scrapboard", newBoard);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateScrapboard_BoardAlreadyExists_BadRequest()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = $"test1@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 2,
        });

        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "Test List",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21),
            HasScrapboard = true,
        });
        await context.SaveChangesAsync();
        var newBoard = new NewScrapboardRequest
        {
            TripId = 2,
        };

        var message = await HttpClient.PostAsJsonAsync("/api/scrapboard", newBoard);
        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateScrapboard_NotAuthorised_Unauthorized()
    {
        int userId = 1;
        int tripId = 1;
        await SetupUserAndTrip(userId,tripId);

        await using var context = DbContextFactory.CreateDbContext();
        
        context.Trips.Add(new Trip
        {
            Id = tripId+1,
            Title = "Test List",
            UserId = userId+1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = tripId+1,
        });
        await context.SaveChangesAsync();
        var newBoard = new NewScrapboardRequest
        {
            TripId = 2,
        };

        var message = await HttpClient.PostAsJsonAsync("/api/scrapboard", newBoard);
        message.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}