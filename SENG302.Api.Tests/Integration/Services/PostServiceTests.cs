using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Services;

public class PostServiceTests : BaseIntegrationTestFixture
{
    byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    /// <summary>
    /// Creates a mock file with a given filename, content, and type
    /// </summary>
    /// <param name="filename">The file name for the mock file</param>
    /// <param name="content">What bytes are in the file</param>
    /// <param name="mimeType">The mime type for the mock file</param>
    /// <returns>The mock file</returns>
    private IFormFile GetMockFile(string filename, Byte[] content, string mimeType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(),
            ContentType = mimeType
        };
    }

    private IAlbumService _albumService => ServiceProvider.GetRequiredService<IAlbumService>();
    private IFileService _fileService => ServiceProvider.GetRequiredService<IFileService>();
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
            StartDate = new DateOnly(2017, 10, 08),
            EndDate = new DateOnly(2017, 10, 21)
        });
        context.Albums.Add(new Album
        {
            TripId = tripId,
            PhotoIds = [],
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
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            creationTime = DateTimeOffset.UtcNow
        });
        context.Posts.Add(new Post
        {
            TripId = 2,
            Title = "User 2 post",
            Content = "desc",
            OsmId = 292806332,
            LocationName = "Christchurch",
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
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
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
        };

        var post = await ServiceUnderTest.CreateNewPostAsync(request, 1, []);

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
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
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
    public async Task CreateNewPostAsync_InvalidLocation_ThrowsAndDoesNotPersist()
    {
        await SetupUserAndTrip(1, 1);

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "Valid title",
            Content = "Valid content",
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            OsmId = 0
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.CreateNewPostAsync(request, 1, [])
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
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.CreateNewPostAsync(request, 1, [])
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
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
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
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
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
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            OsmId = 0
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.UpdatePostAsync(request, 1, new List<IFormFile>())
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

    [Fact]
    public async Task DeleteTrip_ExistingPost_DeletesSinglePosts()
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
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
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

        await ServiceUnderTest.DeleteAllPostOfTrip(1);

        await using var assertContext =
            DbContextFactory.CreateDbContext();

        var postExists = await assertContext.Posts
            .AnyAsync(post => post.TripId == 1);

        var locationExists = await assertContext.Locations
            .AnyAsync(location => location.PostId == postId);

        postExists.ShouldBeFalse();
        locationExists.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteTrip_ExistingPosts_DeletesMultiplePosts()
    {
        await SetupUserAndTrip(1, 1);

        int postId1;
        int postId2;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            var post1 = new Post
            {
                TripId = 1,
                Title = "Post to delete",
                Content = "Post content",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = []
            };

            context.Posts.Add(post1);
            await context.SaveChangesAsync();

            postId1 = post1.PostId;

            var post2 = new Post
            {
                TripId = 1,
                Title = "Post to delete",
                Content = "Post content",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = []
            };

            context.Posts.Add(post2);

            await context.SaveChangesAsync();

            postId2 = post2.PostId;

            context.Locations.Add(new Location
            {
                PostId = postId2,
                OsmId = 123,
                Lat = "-43.532",
                Lon = "172.636",
                Name = "Christchurch",
                DisplayName = "Christchurch, New Zealand"
            });

            context.Locations.Add(new Location
            {
                PostId = postId1,
                OsmId = 123,
                Lat = "-43.532",
                Lon = "172.636",
                Name = "Christchurch",
                DisplayName = "Christchurch, New Zealand"
            });

            await context.SaveChangesAsync();
        }

        await ServiceUnderTest.DeleteAllPostOfTrip(1);

        await using var assertContext =
            DbContextFactory.CreateDbContext();

        var postExists = await assertContext.Posts
            .AnyAsync(post => post.TripId == 1);

        var locationExists = await assertContext.Locations
            .AnyAsync(location => location.PostId == postId1 || location.PostId == postId2);

        postExists.ShouldBeFalse();
        locationExists.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteTrip_ExistingPosts_DeletesOnlyTripPosts()
    {
        await SetupUserAndTrip(1, 1);

        int postId1;
        int postId2;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Trips.Add(new Trip
            {
                Id = 2,
                Title = "Test List",
                UserId = 1,
                Description = "Test",
                StartDate = new DateOnly(2017, 10, 08),
                EndDate = new DateOnly(2017, 10, 21)
            });

            var post1 = new Post
            {
                TripId = 1,
                Title = "Post to delete",
                Content = "Post content",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = []
            };

            context.Posts.Add(post1);
            await context.SaveChangesAsync();

            postId1 = post1.PostId;

            var post2 = new Post
            {
                TripId = 2,
                Title = "Post to delete",
                Content = "Post content",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = []
            };

            context.Posts.Add(post2);

            await context.SaveChangesAsync();

            postId2 = post2.PostId;

            context.Locations.Add(new Location
            {
                PostId = postId1,
                OsmId = 123,
                Lat = "-43.532",
                Lon = "172.636",
                Name = "Christchurch",
                DisplayName = "Christchurch, New Zealand"
            });

            context.Locations.Add(new Location
            {
                PostId = postId2,
                OsmId = 123,
                Lat = "-43.532",
                Lon = "172.636",
                Name = "Christchurch",
                DisplayName = "Christchurch, New Zealand"
            });

            await context.SaveChangesAsync();
        }

        await ServiceUnderTest.DeleteAllPostOfTrip(1);

        await using var assertContext =
            DbContextFactory.CreateDbContext();

        var post1Exists = await assertContext.Posts
            .AnyAsync(post => post.TripId == 1);

        var location1Exists = await assertContext.Locations
            .AnyAsync(location => location.PostId == postId1);

        post1Exists.ShouldBeFalse();
        location1Exists.ShouldBeFalse();

        var post2Exists = await assertContext.Posts
            .AnyAsync(post => post.TripId == 2);

        var location2Exists = await assertContext.Locations
            .AnyAsync(location => location.PostId == postId1 || location.PostId == postId2);

        post2Exists.ShouldBeTrue();
        location2Exists.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteTrip_NoPosts_RunsSuccessfully()
    {
        await SetupUserAndTrip(1, 1);


        await Should.NotThrowAsync(ServiceUnderTest.DeleteAllPostOfTrip(1));
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
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
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
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            OsmId = 0
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => ServiceUnderTest.UpdatePostAsync(request, 1, new List<IFormFile>())
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
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
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
                () => ServiceUnderTest.UpdatePostAsync(request, 1, new List<IFormFile>())
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

    [Fact]
    public async Task DeletePost_ExistingPostWithImage_DeletesImageAndPostAndRemovesFromAlbum()
    {
        await SetupUserAndTrip(1, 1);

        int postId;
        var fileId = 0;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            var mockFileContent = File.ReadAllBytes("resources/panda.webp");

            var mockFile = GetMockFile(
                "mocka",
                mockFileContent,
                "image/webp");

            var fileEntity = await _fileService.SaveFileAsync(mockFile, 0);
            fileId = fileEntity.Id;
            var post = new Post
            {
                TripId = 1,
                Title = "Post to delete",
                Content = "Post content",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = [fileEntity.Id]
            };

            context.Posts.Add(post);
            var currentAlbum = await _albumService.GetAlbumByIdAsync(1);

            currentAlbum.PhotoIds.Add(fileEntity.Id);
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
        var file = await _fileService.GetFileByIdAsync(fileId);
        file.ShouldNotBeNull();
        await ServiceUnderTest.DeletePostByIdAsync(postId);
        var album = await _albumService.GetAlbumByIdAsync(1);
        album.PhotoIds.Count.ShouldBe(0);
        await using var assertContext =
            DbContextFactory.CreateDbContext();

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await _fileService.GetFileByIdAsync(fileId);
        });
    }

    [Fact]
    public async Task DeletePhotoFromPost_ExistingPostWithImage_DeletesImagePostRemains()
    {
        await SetupUserAndTrip(1, 1);

        int postId;
        var fileId = 0;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            var mockFileContent = File.ReadAllBytes("resources/panda.webp");

            var mockFile = GetMockFile(
                "mocka",
                mockFileContent,
                "image/webp");

            var fileEntity = await _fileService.SaveFileAsync(mockFile, 1);
            fileId = fileEntity.Id;
            var post = new Post
            {
                TripId = 1,
                Title = "del post",
                Content = "dfsdofdskjofdsjnlksdfjlnsdfjnlfsdjlsdljkndsfljkndfsljdfsjldfsljk",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = [fileEntity.Id]
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

        var file = await _fileService.GetFileByIdAsync(fileId);
        file.ShouldNotBeNull();

        await ServiceUnderTest.DeletePhotoFromPostAsync(postId, fileId);

        await using var assertContext = DbContextFactory.CreateDbContext();

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await _fileService.GetFileByIdAsync(fileId);
        });

        await ServiceUnderTest.GetPostAsync(postId).ShouldNotThrowAsync();
    }

    [Fact]
    public async Task DeletePhotoFromPost_ExistingPostWithImage_DeletesImageFromAlbum()
    {
        await SetupUserAndTrip(1, 1);

        int postId;
        var fileId = 0;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            var mockFileContent = File.ReadAllBytes("resources/panda.webp");

            var mockFile = GetMockFile(
                "mocka",
                mockFileContent,
                "image/webp");

            var fileEntity = await _fileService.SaveFileAsync(mockFile, 1);
            fileId = fileEntity.Id;
            var post = new Post
            {
                TripId = 1,
                Title = "del post",
                Content = "dfsdofdskjofdsjnlksdfjlnsdfjnlfsdjlsdljkndsfljkndfsljdfsjldfsljk",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = [fileEntity.Id]
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

        var file = await _fileService.GetFileByIdAsync(fileId);
        file.ShouldNotBeNull();

        await ServiceUnderTest.DeletePhotoFromPostAsync(postId, fileId);

        await using var assertContext = DbContextFactory.CreateDbContext();

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await _fileService.GetFileByIdAsync(fileId);
        });

        var album = await _albumService.GetAlbumByIdAsync(1);
        album.PhotoIds.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DeletePhotoFromPost_NonExistentPost_KeyNotFoundException()
    {
        await SetupUserAndTrip(1, 1);     

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.DeletePhotoFromPostAsync(0, 0);
        });
    }

    [Fact]
    public async Task DeletePhotoFromPost_PhotoNotOnPost_ThrowsKeyNotFoundAndFileRemains()
    {
        await SetupUserAndTrip(1, 1);

        int postId;
        int fileId;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            var mockFileContent = File.ReadAllBytes("resources/panda.webp");

            var mockFile = GetMockFile(
                "mocka",
                mockFileContent,
                "image/webp");

            var fileEntity = await _fileService.SaveFileAsync(mockFile, 1);
            fileId = fileEntity.Id;

            var post = new Post
            {
                TripId = 1,
                Title = "where photo",
                Content = "sdfodsiofdsfidsmfdsmsdfoimfiodsmfdiosmfsiomdsfiofmdsi",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = []
            };

            context.Posts.Add(post);
            await context.SaveChangesAsync();

            postId = post.PostId;
        }

        var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.DeletePhotoFromPostAsync(postId, fileId);
        });

        exception.Message.ShouldBe("This photo does not belong to this post.");

        var file = await _fileService.GetFileByIdAsync(fileId);
        file.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeletePhotoFromPost_PostHasNoPhotos_ThrowsKeyNotFound()
    {
        await SetupUserAndTrip(1, 1);

        int postId;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            var post = new Post
            {
                TripId = 1,
                Title = "no photos",
                Content = "asijdasiodasiomdsaiomdsaiomdasoijdasijadsoiadsjoidas",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = []
            };

            context.Posts.Add(post);
            await context.SaveChangesAsync();

            postId = post.PostId;
        }

        var exception = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.DeletePhotoFromPostAsync(postId, 999);
        });

        exception.Message.ShouldBe("This photo does not belong to this post.");
    }


    [Fact]
    public async Task DeletePhotoFromPost_PostHasMultiplePhotos_OnlyDeletesTargetedPhoto()
    {
        await SetupUserAndTrip(1, 1);

        int postId;
        int fileIdToDelete;
        int fileIdToKeep;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            var mockFileContent = File.ReadAllBytes("resources/panda.webp");

            var fileToDelete = await _fileService.SaveFileAsync(
                GetMockFile("removed", mockFileContent, "image/webp"), 1);
            var fileToKeep = await _fileService.SaveFileAsync(
                GetMockFile("keeped", mockFileContent, "image/webp"), 1);

            fileIdToDelete = fileToDelete.Id;
            fileIdToKeep = fileToKeep.Id;

            var post = new Post
            {
                TripId = 1,
                Title = "lil calm post",
                Content = "adiasodmoidmsamdiosamdoajdoimaodis",
                OsmId = 1,
                LocationName = "Christchurch",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                PhotoIds = [fileIdToDelete, fileIdToKeep]
            };

            context.Posts.Add(post);
            await context.SaveChangesAsync();

            postId = post.PostId;
        }

        await ServiceUnderTest.DeletePhotoFromPostAsync(postId, fileIdToDelete);

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await _fileService.GetFileByIdAsync(fileIdToDelete);
        });

        var keptFile = await _fileService.GetFileByIdAsync(fileIdToKeep);
        keptFile.ShouldNotBeNull();

        var updatedPost = await ServiceUnderTest.GetPostAsync(postId);
        updatedPost.ShouldNotBeNull();
        updatedPost!.PhotoIds.ShouldNotContain(fileIdToDelete);
        updatedPost.PhotoIds.ShouldContain(fileIdToKeep);
    }

    [Fact]
    public async Task CreateNewPostAsync_WithImage_ImagesAddedToAlbum()
    {
        await SetupUserAndTrip(1, 1);
        SetupGeoMock();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "test post",
            Content = "description",
            OsmId = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };
        
        var image = GetMockFile(
            "test.png",
            pngBytes,
            "image/png"
        );

        var post = await ServiceUnderTest.CreateNewPostAsync(
            request,
            1,
            [image]
        );

        var album = await context.Set<Album>().Where(album => album.TripId == 1).FirstOrDefaultAsync();
        
        album.PhotoIds.Count.ShouldBe(1);
        album.PhotoIds.ShouldContain(post.PhotoIds[0]);
    }
    
    [Fact]
    public async Task UpdatePostAsync_WithNewImages_ImagesAddedToAlbum()
    {
        await SetupUserAndTrip(1, 1);
        SetupGeoMock();
        
        await using var context = DbContextFactory.CreateDbContext();

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "original post",
            Content = "description",
            OsmId = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };

        var post = await ServiceUnderTest.CreateNewPostAsync(request, 1, []);
        
        await context.SaveChangesAsync();

        var updateRequest = new UpdatePostRequest
        {
            Id = post.PostId,
            TripId = post.TripId,
            Title = "updated title",
            Content = "updated content",
            OsmId = post.OsmId,
            Date = post.Date
        };

        byte[] pngBytes =
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        var image = GetMockFile(
            "test.png",
            pngBytes,
            "image/png"
        );

        var updatedPost = await ServiceUnderTest.UpdatePostAsync(updateRequest, 1, [image]);
        
        await context.SaveChangesAsync();

        var album = await context.Set<Album>().Where(album => album.TripId == 1).FirstOrDefaultAsync();
        
        album.PhotoIds.Count.ShouldBe(1);
        album.PhotoIds.ShouldContain(updatedPost.PhotoIds[0]);
    }

    [Fact]
    public async Task CreateNewPostAsync_WithImage_QueuesImageForTagging()
    {
        await SetupUserAndTrip(1, 1);
        SetupGeoMock();

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "test post",
            Content = "description",
            OsmId = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };

        var image = GetMockFile(
            "test.png",
            pngBytes,
            "image/png"
        );

        var post = await ServiceUnderTest.CreateNewPostAsync(
            request,
            1,
            [image]
        );

        post.PhotoIds.ShouldNotBeNull();
        post.PhotoIds.Count.ShouldBe(1);

        await ImageTaggingQueueMock.Received(1).QueueImageTaggingAsync(
            Arg.Any<ImageTaggingJob>()
        );
    }
    


    [Fact]
    public async Task CreateNewPostAsync_WithoutImages_DoesNotQueueImageTagging()
    {
        await SetupUserAndTrip(1, 1);
        SetupGeoMock();

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "test post with no images",
            Content = "description",
            OsmId = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };

        await ServiceUnderTest.CreateNewPostAsync(request, 1, []);

        await ImageTaggingQueueMock.DidNotReceive().QueueImageTaggingAsync(
            Arg.Any<ImageTaggingJob>()
        );
    }

    [Fact]
    public async Task UpdatePostAsync_WithoutNewImages_DoesNotQueueImageTagging()
    {
        await SetupUserAndTrip(1, 1);
        SetupGeoMock();

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "original post",
            Content = "description",
            OsmId = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };

        var image = GetMockFile(
            "test.png",
            pngBytes,
            "image/png"
        );

        var post = await ServiceUnderTest.CreateNewPostAsync(
            request,
            1,
            [image]
        );

        ImageTaggingQueueMock.ClearReceivedCalls();

        var updateRequest = new UpdatePostRequest
        {
            Id = post.PostId,
            TripId = post.TripId,
            Title = "Updated title",
            Content = "Updated content",
            OsmId = post.OsmId,
            Date = post.Date
        };

        await ServiceUnderTest.UpdatePostAsync(updateRequest, 1, []);

        await ImageTaggingQueueMock.DidNotReceive().QueueImageTaggingAsync(
            Arg.Any<ImageTaggingJob>()
        );
    }

    [Fact]
    public async Task UploadPhotosToPostAsync_WithNewImage_QueuesImageForTagging()
    {
        await SetupUserAndTrip(1, 1);
        SetupGeoMock();

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "original post",
            Content = "description",
            OsmId = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };

        var post = await ServiceUnderTest.CreateNewPostAsync(request, 1, []);

        var updateRequest = new UpdatePostRequest
        {
            Id = post.PostId,
            TripId = post.TripId,
            Title = "updated title",
            Content = "updated content",
            OsmId = post.OsmId,
            Date = post.Date
        };

        byte[] pngBytes =
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        var image = GetMockFile(
            "test.png",
            pngBytes,
            "image/png"
        );

        var updatedPost = await ServiceUnderTest.UpdatePostAsync(updateRequest, 1, [image]);

        updatedPost.ShouldNotBeNull();
        updatedPost.PhotoIds.ShouldNotBeNull();
        updatedPost.PhotoIds.Count.ShouldBe(1);

        await ImageTaggingQueueMock.Received(1).QueueImageTaggingAsync(
            Arg.Any<ImageTaggingJob>()
        );
    }
    
    
}
