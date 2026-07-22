using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class PostControllerTests : BaseIntegrationTestFixture
{
    public PostControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory) { }

    [Fact]
    public async Task CreatePost_ValidData_Ok()
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
        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "test tasklist",
            UserId = 1,
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        });
        
        await context.SaveChangesAsync();

        var message = await HttpClient.PostAsJsonAsync("api/post", new Post
        {
            TripId = 1,
            Title = "name",
            Content = "test",
            creationTime = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(1)),
            Location = "France"
        });
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
    

    [Fact]
    public async Task GetAllTasks_AuthenticatedUser_ReturnsOnlyUsersTask()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Users.Add(new User { Id = 2, Email = "other@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My list", UserId = 1, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21)});
        context.Trips.Add(new Trip { Id = 2, Title = "Other list", UserId = 2, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });
        context.Posts.Add(new Post { PostId = 0, TripId = 1, Title = "My task", Content = "", Location = "location 1"});
        context.Posts.Add(new Post { PostId = 1, TripId = 1, Title = "Ti  tle", Content = "", Location = "location 1" });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<Post>>();
        tasks!.Count.ShouldBe(1);
        tasks.First().Title.ShouldBe("My task");
    }

    [Fact]
    public async Task GetAllTasks_NoTasks_ReturnsEmptyList()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test User", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My List", UserId = 1, Description = "", StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21)});
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<Post>>();
        tasks!.ShouldBeEmpty();
    }


}