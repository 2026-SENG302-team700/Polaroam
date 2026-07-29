using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
            LocationName = "Christchurch",
            Date = DateTime.UtcNow,
            creationTime = DateTimeOffset.UtcNow
        });
        context.Posts.Add(new Post
        {
            TripId = 2,
            Title = "User 2 post",
            Content = "desc",
            OsmId = 292806332,
            LocationName = "Christchurch",
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
            Date = DateTime.UtcNow.AddDays(-2),
        };

        var post = await ServiceUnderTest.CreateNewPostAsync(request, [], 1);

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
            LocationName = "Christchurch",
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
            Date = DateTime.UtcNow.AddDays(-2),
        };

        var post = await ServiceUnderTest.CreateNewPostAsync(request, [], 1);

        List<int> photoIds = [1, 2];
        var updatedPost = await ServiceUnderTest.AddPhotosToPost(photoIds, post.PostId);

        updatedPost.ShouldNotBeNull();
        updatedPost.PhotoIds.ShouldBe(photoIds);
        updatedPost.PostId.ShouldBe(post.PostId);
    }

    [Fact]
    public async Task CreateNewPostAsync_InvalidLocation_ThrowsAndDoesNotPersist()
    {
        await SetupUserAndTrip(1, 1);

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "Valid title",
            Content = "Valid content",
            Date = DateTimeOffset.Now.AddDays(-1),
            OsmId = 0
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.CreateNewPostAsync(request, [], 1)
            );

        exception.Errors["location"]
            .ShouldContain("This isn't a valid location");

        await using var context = DbContextFactory.CreateDbContext();
        (await context.Posts.AnyAsync()).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(129)]
    public async Task CreateNewPostAsync_InvalidTitle_ThrowsValidationExceptionAndDoesNotPersist(int titleLength)
    {
        await SetupUserAndTrip(1, 1);

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = new string('a', titleLength),
            Content = "SOme content",
            OsmId = 1,
            Date = DateTime.UtcNow.AddDays(-2),
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.CreateNewPostAsync(request, [], 1)
            );

        exception.Errors.ShouldContainKey("title");
        exception.Errors["title"].ShouldContain(
            "Title is required and must be between 3 and 128 characters long"
        );

        await using var context = DbContextFactory.CreateDbContext();

        var postExists = await context.Posts.AnyAsync();

        postExists.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletePost_ExistingPost_DeletesPostAndLocation()
    {
        await SetupUserAndTrip(1, 1);

        int postId;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            var post = new Post
            {
                TripId = 1,
                Title = "Post to delete",
                Content = "Post content",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateTimeOffset.Now.AddDays(-1),
                PhotoIds = []
            };

            context.Posts.Add(post);
            await context.SaveChangesAsync();

            postId = post.PostId;

            context.Locations.Add(new Location
            {
                PostId = postId,
                OsmId = 123,
                Lat = "-43.532",
                Lon = "172.636",
                Name = "Christchurch",
                DisplayName = "Christchurch, New Zealand"
            });

            await context.SaveChangesAsync();
        }

        await ServiceUnderTest.DeletePostByIdAsync(postId);

        await using var assertContext =
            DbContextFactory.CreateDbContext();

        var postExists = await assertContext.Posts
            .AnyAsync(post => post.PostId == postId);

        var locationExists = await assertContext.Locations
            .AnyAsync(location => location.PostId == postId);

        postExists.ShouldBeFalse();
        locationExists.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletePost_NonExistentPost_ThrowsArgumentException()
    {
        var exception = await Should.ThrowAsync<ArgumentException>(
            () => ServiceUnderTest.DeletePostByIdAsync(999)
        );

        exception.Message.ShouldBe(
            "Post with the provided ID does not exist.");
    }

    [Fact]
    public async Task UpdatePostAsync_InvalidLocation_ThrowsAndDoesNotUpdatePost()
    {
        await SetupUserAndTrip(1, 1);

        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Posts.Add(new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "og title",
                Content = "og content",
                Date = DateTimeOffset.Now.AddDays(-2),
                OsmId = 123,
                LocationName = "Christchurch",
                PhotoIds = []
            });

            await context.SaveChangesAsync();
        }

        var request = new UpdatePostRequest
        {
            Id = 1,
            TripId = 1,
            Title = "updated title",
            Content = "updated content",
            Date = DateTimeOffset.Now.AddDays(-1),
            OsmId = 0
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.UpdatePostAsync(request)
            );

        exception.Errors["location"]
            .ShouldContain("This isn't a valid location");

        await using var assertContext = DbContextFactory.CreateDbContext();

        var post = await assertContext.Posts.FindAsync(1);

        post.ShouldNotBeNull();
        post.Title.ShouldBe("og title");
        post.Content.ShouldBe("og content");
        post.OsmId.ShouldBe(123);
        post.LocationName.ShouldBe("Christchurch");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(129)]
    public async Task UpdatePostAsync_InvalidTitleLength_ThrowsAndDoesNotUpdatePost(int titleLength)
    {
        await SetupUserAndTrip(1, 1);

        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Posts.Add(new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "og title",
                Content = "og content",
                Date = DateTimeOffset.Now.AddDays(-2),
                OsmId = 123,
                LocationName = "Christchurch",
                PhotoIds = []
            });

            await context.SaveChangesAsync();
        }

        var request = new UpdatePostRequest
        {
            Id = 1,
            TripId = 1,
            Title = new string('a', titleLength),
            Content = "updated content",
            Date = DateTimeOffset.Now.AddDays(-1),
            OsmId = 0
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.UpdatePostAsync(request)
            );

        exception.Errors["title"]
            .ShouldContain("Title is required and must be between 3 and 128 characters long");

        await using var assertContext = DbContextFactory.CreateDbContext();

        var post = await assertContext.Posts.FindAsync(1);

        post.ShouldNotBeNull();
        post.Title.ShouldBe("og title");
        post.Content.ShouldBe("og content");
        post.OsmId.ShouldBe(123);
        post.LocationName.ShouldBe("Christchurch");
    }

    [Fact]
    public async Task UpdatePostAsync_InvalidDateFormat_ThrowsAndDoesNotUpdatePost()
    {
        await SetupUserAndTrip(1, 1);

        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Posts.Add(new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "og title",
                Content = "og content",
                Date = DateTimeOffset.Now.AddDays(-2),
                OsmId = 123,
                LocationName = "Christchurch",
                PhotoIds = []
            });

            await context.SaveChangesAsync();
        }

        var request = new UpdatePostRequest
        {
            Id = 1,
            TripId = 1,
            Title = "updated title",
            Content = "updated content",
            Date = null,
            OsmId = 123
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.UpdatePostAsync(request)
            );

        exception.Errors["date"][0]
            .ShouldContain("Invalid date, date must be formatted");

        await using var assertContext = DbContextFactory.CreateDbContext();

        var post = await assertContext.Posts.FindAsync(1);

        post.ShouldNotBeNull();
        post.Title.ShouldBe("og title");
        post.Content.ShouldBe("og content");
        post.OsmId.ShouldBe(123);
        post.LocationName.ShouldBe("Christchurch");
    }
}