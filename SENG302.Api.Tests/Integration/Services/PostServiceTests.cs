using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Services;

public class PostServiceTests : BaseIntegrationTestFixture
{
    private IPostService ServiceUnderTest => ServiceProvider.GetRequiredService<IPostService>();
    public PostServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory) { }

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
        context.Trips.Add(new Trip
        {
            Id = tripId,
            Title = "Test List",
            UserId = userId,
            Description = "Test",
            StartDate = new DateTime(2017, 10, 08),
            EndDate = new DateTime(2017, 10, 21)
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllPostsAsync_NoPosts_ReturnsEmpty()
    {
        await SetupUserAndTrip(1, 1);
        var posts = await ServiceUnderTest.GetAllPostsAsync(1);
        posts.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllPostsAsync_WithPosts_ReturnsOnlyUsersPost()
    {
        // setup two users and a trip
        await SetupUserAndTrip(1, 1);
        await SetupUserAndTrip(2, 2);

        await using var context = DbContextFactory.CreateDbContext();
        context.Posts.Add(new Post
        {
            TripId = 1,
            Title = "User 1 post",
            Content = "desc",
            Location = "location",
            Date = DateTime.UtcNow,
            creationTime = DateTimeOffset.UtcNow
        });
        context.Posts.Add(new Post
        {
            TripId = 2,
            Title = "User 2 post",
            Content = "desc",
            Location = "location",
            Date = DateTime.UtcNow,
            creationTime = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var posts = await ServiceUnderTest.GetAllPostsAsync(1);

        posts.Count().ShouldBe(1);
        posts.First().Title.ShouldBe("User 1 post");
    }

    [Fact]
    public async Task CreateNewPostAsync_ValidRequest_ReturnsCreatedPost()
    {
        await SetupUserAndTrip(1, 1);

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "New post",
            Content = "Some description",
            Location = "Some location",
            Date = DateTime.UtcNow.AddDays(2),
        };

        var post = await ServiceUnderTest.CreateNewPostAsync(request);

        post.Title.ShouldBe("New post");
        post.TripId.ShouldBe(1);
    }


    [Fact]
    public async Task GetPostAsync_ValidId_ReturnsPost()
    {
        await SetupUserAndTrip(1, 1);

        await using var context = DbContextFactory.CreateDbContext();
        var post = new Post
        {
            TripId = 1,
            Title = "Fetch me",
            Content = "desc",
            Location = "location",
            Date = DateTime.UtcNow,
            creationTime = DateTimeOffset.UtcNow
        };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var result = await ServiceUnderTest.GetPostAsync(post.PostId);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Fetch me");
    }

    [Fact]
    public async Task GetPostAsync_InvalidId_ReturnsNull()
    {
        var result = await ServiceUnderTest.GetPostAsync(-1);
        result.ShouldBeNull();
    }
}