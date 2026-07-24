using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
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

    private void SetupGeoMock()
    {
        GeoServiceMock.GetLocationAsync(Arg.Any<long>()).Returns(new GeoLocationSearchResult
        {
            osm_id = 1,
            lat = "1",
            lon = "1",
            display_name = "Auckland",
            name = "Auckland, New Zealand"
        });
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
            OsmId = 292806332,
            Date = DateTime.UtcNow,
            creationTime = DateTimeOffset.UtcNow
        });
        context.Posts.Add(new Post
        {
            TripId = 2,
            Title = "User 2 post",
            Content = "desc",
            OsmId = 292806332,
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
        SetupGeoMock();

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "New post",
            Content = "Some description",
            OsmId = 292806332,
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
            OsmId = 292806332,
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

    [Fact]
    public async Task AddPhotoToPost_InvalidPostId_ReturnsNull()
    {
        var result = await ServiceUnderTest.AddPhotosToPost([1, 2], 1);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task AddPhotoToPost_ValidRequest_ReturnsPost()
    {
        await SetupUserAndTrip(1, 1);
        SetupGeoMock();

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "New post",
            Content = "Some description",
            OsmId = 1,
            Date = DateTime.UtcNow.AddDays(2),
        };

        var post = await ServiceUnderTest.CreateNewPostAsync(request);

        List<int> photoIds = [1, 2];
        var updatedPost = await ServiceUnderTest.AddPhotosToPost(photoIds, post.PostId);
        
        updatedPost.ShouldNotBeNull();
        updatedPost.PhotoIds.ShouldBe(photoIds);
        updatedPost.PostId.ShouldBe(post.PostId);
    }
}