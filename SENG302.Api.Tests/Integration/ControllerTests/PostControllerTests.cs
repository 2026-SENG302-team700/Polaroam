using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class PostControllerTests : BaseIntegrationTestFixture
{
    public PostControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory) { }

    private void SetupGeoMock()
    {
        GeoServiceMock.GetLocationAsync(1).Returns(new GeoLocationSearchResult
        {
            OsmId = 1,
            Latitude = "1",
            Longitude = "1",
            DisplayName = "Auckland",
            Name = "Auckland, New Zealand"
        });
    }

    /// <summary>
    /// Sets up a user, trip and post for testing.
    /// All Ids are set to 1.
    /// </summary>
    private async Task SetupUserTripAndPost()
    {
        await using var context = DbContextFactory.CreateDbContext();
        SetupGeoMock();
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Albums.Add(new Album { TripId = 1, PhotoIds = [] });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "Christchurch", });
        await context.SaveChangesAsync();
    }

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
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });

        await context.SaveChangesAsync();

        SetupGeoMock();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("name"), "Title");
        content.Add(new StringContent("test"), "Content");
        content.Add(new StringContent("1"), "osmId");
        content.Add(new StringContent(DateTimeOffset.UtcNow.ToString("s") + "Z"), "Date");

        var message = await HttpClient.PostAsync("/api/post", content);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }


    [Fact]
    public async Task GetAllPosts_AuthenticatedUser_ReturnsOnlyUsersPost()
    {
        await using var context = DbContextFactory.CreateDbContext();
        // Add two users
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Users.Add(new User { Id = 2, Email = "other@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });

        // Add a trip to each user
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Trips.Add(new Trip { Id = 2, Title = "Other trip", UserId = 2, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Albums.Add(new Album
        {
            TripId = 2,
            PhotoIds = [],
        });
        // Add 1 post to each trip
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.Posts.Add(new Post { TripId = 2, Title = "Ti  tle", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var posts = await response.Content.ReadFromJsonAsync<List<Post>>();
        posts!.Count.ShouldBe(1);
        posts.First().Title.ShouldBe("My post");
    }

    [Fact]
    public async Task GetAllPosts_NoPosts_ReturnsEmpty()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test User", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My Trip", UserId = 1, Description = "", StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var posts = await response.Content.ReadFromJsonAsync<List<Post>>();
        posts!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllPostsFromTrip_ValidRequest_ReturnsPostsFromRequestedTripOnly()
    {
        await using var context = DbContextFactory.CreateDbContext();
        // Add two users
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Users.Add(new User { Id = 2, Email = "other@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });

        // Add a trip to each user
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Trips.Add(new Trip { Id = 2, Title = "Other trip", UserId = 2, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Albums.Add(new Album
        {
            TripId = 2,
            PhotoIds = [],
        });
        // Add 1 post to each trip
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.Posts.Add(new Post { TripId = 2, Title = "Ti  tle", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/trip/post/1");
        var posts = await response.Content.ReadFromJsonAsync<List<Post>>();

        posts.ShouldNotBeNull();
        posts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreatePost_FutureDate_ReturnBadRequest()
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
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });

        await context.SaveChangesAsync();

        SetupGeoMock();
        var message = await HttpClient.PostAsJsonAsync("/api/post", new Post
        {
            TripId = 1,
            Title = "name",
            Content = "test",
            CreationTime = DateTimeOffset.UtcNow,
            OsmId = 1,
            LocationName = "Christchurch",
            Date = DateTimeOffset.UtcNow.AddDays(1)
        });
        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_NullDate_ReturnBadRequest()
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
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });

        await context.SaveChangesAsync();

        SetupGeoMock();
        var message = await HttpClient.PostAsJsonAsync("/api/post", new Post
        {
            TripId = 1,
            Title = "name",
            Content = "test",
            CreationTime = DateTimeOffset.UtcNow,
            OsmId = 1,
            LocationName = "Christchurch",
            Date = null
        });
        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_NoContentAndNotAllowed_Ok()
    {
        // Create DB
        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        await context.SaveChangesAsync();

        SetupGeoMock();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("name"), "Title");
        content.Add(new StringContent("test"), "Content");
        content.Add(new StringContent("1"), "osmId");
        content.Add(new StringContent(DateTimeOffset.UtcNow.ToString("s") + "Z"), "Date");

        var message = await HttpClient.PostAsync("/api/post", content);

        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePost_NoContentAndAllowed_Ok()
    {
        // Create DB
        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        await context.SaveChangesAsync();

        SetupGeoMock();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("name"), "Title");
        content.Add(new StringContent("test"), "Content");
        content.Add(new StringContent("1"), "osmId");
        content.Add(new StringContent(DateTimeOffset.UtcNow.ToString("s") + "Z"), "Date");
        content.Add(new StringContent("true"), "NoContentAllowed");

        var message = await HttpClient.PostAsync("/api/post", content);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePost_InvalidTitleLength_ReturnsBadRequestAndDoesNotPersist()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "test@example.com",
                DisplayName = "Test User",
                PasswordKey = "password",
                Country = "NZ"
            });
            context.Albums.Add(new Album
            {
                TripId = 1,
                PhotoIds = [],
            });

            context.Trips.Add(new Trip
            {
                Id = 1,
                Title = "Test trip",
                Description = "Test description",
                UserId = 1,
                StartDate = DateToDateTimeOffset(2026, 7, 1),
                EndDate = DateToDateTimeOffset(2026, 7, 10)
            });

            await context.SaveChangesAsync();
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("ab"), "Title");
        content.Add(new StringContent("Some content"), "Content");
        content.Add(new StringContent("1"), "osmId");
        content.Add(new StringContent(DateToDateTimeOffset(2026, 7, 5).ToString("s") + "Z"), "Date");

        var response = await HttpClient.PostAsync("/api/post", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadAsStringAsync();

        responseBody.ShouldContain("Title is required and must be between 3 and 128 characters long");

        await using var context2 = DbContextFactory.CreateDbContext();

        var postExists =
            await context2.Posts.AnyAsync();

        postExists.ShouldBeFalse();
    }

    [Fact]
    public async Task CreatePost_ContentTooLong_ReturnsBadRequestAndDoesNotPersist()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "test@example.com",
                DisplayName = "Test User",
                PasswordKey = "password",
                Country = "NZ"
            });
            context.Albums.Add(new Album
            {
                TripId = 1,
                PhotoIds = [],
            });
            context.Trips.Add(new Trip
            {
                Id = 1,
                Title = "Test trip",
                Description = "Test description",
                UserId = 1,
                StartDate = DateToDateTimeOffset(2026, 7, 1),
                EndDate = DateToDateTimeOffset(2026, 7, 10)
            });

            await context.SaveChangesAsync();
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("title"), "Title");
        content.Add(new StringContent(new string('a', 8197)), "Content");
        content.Add(new StringContent("1"), "osmId");
        content.Add(new StringContent(DateToDateTimeOffset(2026, 7, 5).ToString("s") + "Z"), "Date");

        var response = await HttpClient.PostAsync("/api/post", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.ShouldContain("Content needs to be less than 8196 characters long");

        await using var context2 = DbContextFactory.CreateDbContext();

        var postExists =
            await context2.Posts.AnyAsync();

        postExists.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdatePost_ValidPhotoUpload_PostContainsPhotos()
    {
        await SetupUserTripAndPost();

        var file = await File.ReadAllBytesAsync("resources/panda.webp");

        using var content = BuildUpdatePostForm(photos: [(file, "panda.webp", "image/webp")]);

        var response = await HttpClient.PutAsync("/api/post/1", content);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var post = await response.Content.ReadFromJsonAsync<Post>();
        post!.PhotoIds.ShouldBe([1]);
    }

    private MultipartFormDataContent BuildPostForm(
        long osmId = 1,
        IEnumerable<(byte[] Bytes, string FileName, string ContentType)>? photos = null)
    {
        var content = new MultipartFormDataContent();

        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("Valid title"), "Title");
        content.Add(new StringContent("Valid content"), "Content");
        content.Add(new StringContent(osmId.ToString()), "osmId");
        content.Add(new StringContent(DateTimeOffset.UtcNow.ToString("s") + "Z"), "Date");

        foreach (var (bytes, fileName, contentType) in photos ?? [])
        {
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "files", fileName);
        }

        return content;
    }

    [Fact]
    public async Task CreatePost_ValidPhotos_Ok()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Users.Add(new User { Id = 1, Email = "vlad@nistor.com", DisplayName = "Vlad Nistor", PasswordKey = "P4$$word", Country = "NZ" });
        await context.SaveChangesAsync();

        SetupGeoMock();

        var pandaBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        using var content = BuildPostForm(photos: [(pandaBytes, "panda.webp", "image/webp")]);

        var response = await HttpClient.PostAsync("/api/post", content);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var post = await response.Content.ReadFromJsonAsync<Post>();
        post!.PhotoIds!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreatePost_MultipleValidPhotos_Ok()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Users.Add(new User { Id = 1, Email = "vlad@nistor.com", DisplayName = "Vlad Nistor", PasswordKey = "P4$$word", Country = "NZ" });
        await context.SaveChangesAsync();

        SetupGeoMock();

        var pandaBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        var pngBytes = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

        using var content = BuildPostForm(photos:
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (pngBytes, "square.png", "image/png")
        ]);

        var response = await HttpClient.PostAsync("/api/post", content);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var post = await response.Content.ReadFromJsonAsync<Post>();
        post!.PhotoIds!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CreatePost_NoPhotos_Ok()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Users.Add(new User { Id = 1, Email = "vlad@nistor.com", DisplayName = "Vlad Nistor", PasswordKey = "P4$$word", Country = "NZ" });
        await context.SaveChangesAsync();

        SetupGeoMock();

        using var content = BuildPostForm();

        var response = await HttpClient.PostAsync("/api/post", content);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var post = await response.Content.ReadFromJsonAsync<Post>();
        post!.PhotoIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreatePost_OneInvalidPhotoOthersOk_BadRequestPostNotCreated()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User { Id = 1, Email = "vlad@nistor.com", DisplayName = "Vlad Nistor", PasswordKey = "P4$$word", Country = "NZ" });
            await context.SaveChangesAsync();
        }

        SetupGeoMock();

        var pandaBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        var invalidBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        using var content = BuildPostForm(photos:
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (invalidBytes, "notreal.png", "image/png")
        ]);

        var response = await HttpClient.PostAsync("/api/post", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = await response.Content.ReadFromJsonAsync<BadRequestValidationListResponse>();
        errors!.Errors["photos"].ShouldContain(e => e.Contains("notreal.png"));

        await using var context2 = DbContextFactory.CreateDbContext();
        (await context2.Posts.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task CreatePost_Over10MBPhoto_BadRequest()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "vlad@nistor.com", DisplayName = "Vlad Nistor", PasswordKey = "P4$$word", Country = "NZ" });
        await context.SaveChangesAsync();

        SetupGeoMock();

        var oversizedBytes = new byte[10000001];

        using var content = BuildPostForm(photos: [(oversizedBytes, "huge.png", "image/png")]);

        var response = await HttpClient.PostAsync("/api/post", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_MultipleInvalidPhotos_BadRequestAllBadFilesListed()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "vlad@nistor.com", DisplayName = "Vlad Nistor", PasswordKey = "P4$$word", Country = "NZ" });
        await context.SaveChangesAsync();

        SetupGeoMock();

        var invalidBytesOne = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var invalidBytesTwo = new byte[] { 0xde, 0xad, 0xbe, 0xef };

        using var content = BuildPostForm(photos:
        [
            (invalidBytesOne, "bad1.png", "image/png"),
            (invalidBytesTwo, "bad2.jpeg", "image/jpeg")
        ]);

        var response = await HttpClient.PostAsync("/api/post", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = await response.Content.ReadFromJsonAsync<BadRequestValidationListResponse>();
        errors!.Errors["photos"].Count.ShouldBe(2);
        errors.Errors["photos"].ShouldContain(e => e.Contains("bad1.png"));
        errors.Errors["photos"].ShouldContain(e => e.Contains("bad2.jpeg"));
    }

    [Fact]
    public async Task UpdatePost_NotOwnerOfPost_ReturnsUnauthorized()
    {
        await using var context = DbContextFactory.CreateDbContext();

        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Users.Add(new User { Id = 2, Email = "other@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 2, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "Christchurch", });
        await context.SaveChangesAsync();

        var file = await File.ReadAllBytesAsync("resources/panda.webp");
        using var content = BuildUpdatePostForm(photos: [(file, "panda.webp", "image/webp")]);

        var response = await HttpClient.PutAsync("/api/post/1", content);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserNameByPostId_ValidRequest_ReturnsDisplayName()
    {
        await using var context = DbContextFactory.CreateDbContext();

        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "Christchurch", });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post/1/user");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldContain("Test user");
    }

    [Fact]
    public async Task GetUserNameByPostId_InvalidPostId_ReturnsNotFoundError()
    {
        await using var context = DbContextFactory.CreateDbContext();

        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "Christchurch", });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post/2/user");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldContain("Post not found");
    }

    [Fact]
    public async Task GetPostPhotoMetadata_ValidPostId_ReturnsPhotoMetaData()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "Christchurch", });
        await context.SaveChangesAsync();

        var file = await File.ReadAllBytesAsync("resources/panda.webp");
        using var uploadForm = BuildUpdatePostForm(photos: [(file, "panda.webp", "image/webp")]);
        await HttpClient.PutAsync("/api/post/1", uploadForm);

        var response = await HttpClient.GetAsync("/api/post/1/images");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var photos = await response.Content.ReadFromJsonAsync<List<PhotoMetadataResponse>>();

        photos.ShouldNotBeNull();
        photos.Count.ShouldBe(1);
        photos.First().FileName.ShouldBe("panda.webp");

    }

    [Fact]
    public async Task GetPostPhotoMetadata_InvalidPostId_ReturnsNotFound()
    {
        await using var context = DbContextFactory.CreateDbContext();

        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "Roumaine" });
        await context.SaveChangesAsync();

        var file = await File.ReadAllBytesAsync("resources/panda.webp");
        using var uploadForm = BuildUpdatePostForm(photos: [(file, "panda.webp", "image/webp")]);
        await HttpClient.PutAsync("/api/post/1", uploadForm);

        var response = await HttpClient.GetAsync("/api/post/3/images");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var photos = await response.Content.ReadAsStringAsync();
        photos.ShouldContain("Post not found");
    }

    [Fact]
    public async Task GetPostPhoto_ValidPostId_ReturnsPhoto()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "my home" });
        await context.SaveChangesAsync();

        var file = await File.ReadAllBytesAsync("resources/panda.webp");
        using var uploadForm = BuildUpdatePostForm(photos: [(file, "panda.webp", "image/webp")]);
        await HttpClient.PutAsync("/api/post/1", uploadForm);

        var response = await HttpClient.GetAsync("/api/post/1/images/1");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBytes = await response.Content.ReadAsByteArrayAsync();

        var fileBytes = await File.ReadAllBytesAsync("resources/panda.webp");

        responseBytes.SequenceEqual(fileBytes).ShouldBeTrue();
    }

    [Fact]
    public async Task GetPostPhoto_PhotoIdIsNull_ReturnsNotFound()
    {
        await using var context = DbContextFactory.CreateDbContext();

        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "my basement" });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post/1/images/1");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var photos = await response.Content.ReadAsStringAsync();
        photos.ShouldContain("Photo not found on this post.");
    }

    [Fact]
    public async Task GetPostPhoto_PhotoIdIsNotPresent_ReturnsNotFound()
    {
        await using var context = DbContextFactory.CreateDbContext();

        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = DateToDateTimeOffset(2025, 11, 21), EndDate = DateToDateTimeOffset(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", PhotoIds = [], OsmId = 1, LocationName = "a mysterious place" });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post/1/images/5");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var photos = await response.Content.ReadAsStringAsync();
        photos.ShouldContain("Photo not found on this post.");
    }

    private MultipartFormDataContent BuildUpdatePostForm(
        long osmId = 1,
        IEnumerable<(byte[] Bytes, string FileName, string ContentType)>? photos = null)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "Id");
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("Valid title"), "Title");
        content.Add(new StringContent("Valid content"), "Content");
        content.Add(new StringContent(osmId.ToString()), "osmId");
        content.Add(new StringContent(DateTimeOffset.UtcNow.ToString("s") + "Z"), "Date");

        foreach (var (bytes, fileName, contentType) in photos ?? [])
        {
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "files", fileName);
        }

        return content;
    }

    [Fact]
    public async Task UpdatePost_AllFields_UpdateSuccessful()
    {
        await SetupUserTripAndPost();

        SetupGeoMock();

        var pandaBytes =
            await File.ReadAllBytesAsync("resources/panda.webp");

        var pngBytes = new byte[]
        {
          0x89, 0x50, 0x4e, 0x47,
          0x0d, 0x0a, 0x1a, 0x0a
        };

        using var updateForm = BuildUpdatePostForm(
            photos:
            [
                (pandaBytes, "panda.webp", "image/webp"),
                (pngBytes, "square.png", "image/png")
            ]);

        var response = await HttpClient.PutAsync(
            "/api/post/1",
            updateForm);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var context =
            DbContextFactory.CreateDbContext();

        var updatedPost = await context.Posts
            .FirstOrDefaultAsync(post => post.PostId == 1);

        updatedPost.ShouldNotBeNull();
        updatedPost.Title.ShouldBe("Valid title");
        updatedPost.Content.ShouldBe("Valid content");
        updatedPost.PhotoIds.ShouldNotBeNull();
        updatedPost.PhotoIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DeletePost_IsOwner_ReturnsNoContentAndDeletesPost()
    {
        int postId;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "ex@example.com",
                DisplayName = "Owner",
                PasswordKey = "password",
                Country = "NZ"
            });

            context.Trips.Add(new Trip
            {
                Id = 1,
                UserId = 1,
                Title = "Test trip",
                Description = "",
                StartDate = DateToDateTimeOffset(2026, 1, 1),
                EndDate = DateToDateTimeOffset(2026, 1, 10)
            });

            var post = new Post
            {
                TripId = 1,
                Title = "Delete",
                Content = "Content",
                OsmId = 1,
                LocationName = "Christchurch",
                PhotoIds = [],
                Date = DateTimeOffset.UtcNow.AddDays(-1)
            };

            context.Posts.Add(post);
            await context.SaveChangesAsync();

            postId = post.PostId;
        }

        var response =
            await HttpClient.DeleteAsync($"/api/post/{postId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await using var assertContext =
            DbContextFactory.CreateDbContext();

        var postExists = await assertContext.Posts
            .AnyAsync(post => post.PostId == postId);

        postExists.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletePost_WrongUser_ReturnsUnauthorizedAndKeepsPost()
    {
        int postId;

        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 2,
                Email = "other@example.com",
                DisplayName = "Other user",
                PasswordKey = "password",
                Country = "NZ"
            });

            context.Trips.Add(new Trip
            {
                Id = 1,
                UserId = 2,
                Title = "Other user trip",
                Description = "",
                StartDate = DateToDateTimeOffset(2026, 1, 1),
                EndDate = DateToDateTimeOffset(2026, 1, 10)
            });

            var post = new Post
            {
                TripId = 1,
                Title = "Dont delete",
                Content = "Content",
                OsmId = 1,
                LocationName = "Christchurch",
                PhotoIds = [],
                Date = DateTimeOffset.UtcNow.AddDays(-1)
            };

            context.Posts.Add(post);
            await context.SaveChangesAsync();

            postId = post.PostId;
        }

        var response =
            await HttpClient.DeleteAsync($"/api/post/{postId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        await using var assertContext =
            DbContextFactory.CreateDbContext();

        var postExists = await assertContext.Posts
            .AnyAsync(post => post.PostId == postId);

        postExists.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletePost_PostDoesNotExist_ReturnsNotFound()
    {
        var response =
            await HttpClient.DeleteAsync("/api/post/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePost_InvalidLocation_ReturnsBadRequestAndDoesNotUpdatePost()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "owner@ex.com",
                DisplayName = "poster",
                PasswordKey = "password",
                Country = "NZ"
            });

            context.Trips.Add(new Trip
            {
                Id = 1,
                UserId = 1,
                Title = "test trip",
                Description = "test description",
                StartDate = DateToDateTimeOffset(2026, 1, 1),
                EndDate = DateToDateTimeOffset(2026, 1, 10)
            });

            context.Posts.Add(new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "og title",
                Content = "og content",
                Date = DateTimeOffset.UtcNow.AddDays(-2),
                OsmId = 123,
                LocationName = "Christchurch",
                PhotoIds = []
            });

            await context.SaveChangesAsync();
        }


        var pandaBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        var pngBytes = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

        using var updateRequest = BuildUpdatePostForm(photos:
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (pngBytes, "square.png", "image/png")
        ]);

        var response = await HttpClient.PutAsync(
            "/api/post/1",
            updateRequest
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.ShouldContain("This isn't a valid location");

        await using var assertContext = DbContextFactory.CreateDbContext();

        var post = await assertContext.Posts.FindAsync(1);

        post.ShouldNotBeNull();
        post.Title.ShouldBe("og title");
        post.Content.ShouldBe("og content");
        post.OsmId.ShouldBe(123);
        post.LocationName.ShouldBe("Christchurch");
    }



    [Fact]
    public async Task UpdatePost_OneInvalidPhotoOthersOk_BadRequestPostNotCreated()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "owner@ex.com",
                DisplayName = "poster",
                PasswordKey = "password",
                Country = "NZ"
            });

            context.Trips.Add(new Trip
            {
                Id = 1,
                UserId = 1,
                Title = "test trip",
                Description = "test description",
                StartDate = DateToDateTimeOffset(2026, 1, 1),
                EndDate = DateToDateTimeOffset(2026, 1, 10)
            });

            context.Posts.Add(new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "og title",
                Content = "og content",
                Date = DateTimeOffset.UtcNow.AddDays(-2),
                OsmId = 123,
                LocationName = "Christchurch",
                PhotoIds = []
            });

            await context.SaveChangesAsync();
        }

        SetupGeoMock();

        var pandaBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        var invalidBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        using var content = BuildUpdatePostForm(photos:
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (invalidBytes, "notreal.png", "image/png")
        ]);

        var response = await HttpClient.PutAsync("/api/post/1", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = await response.Content.ReadFromJsonAsync<BadRequestValidationListResponse>();
        errors!.Errors["photos"].ShouldContain(e => e.Contains("notreal.png"));

    }

    [Fact]
    public async Task UpdatePost_Over10MBPhoto_BadRequest()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "owner@ex.com",
                DisplayName = "poster",
                PasswordKey = "password",
                Country = "NZ"
            });

            context.Trips.Add(new Trip
            {
                Id = 1,
                UserId = 1,
                Title = "test trip",
                Description = "test description",
                StartDate = DateToDateTimeOffset(2026, 1, 1),
                EndDate = DateToDateTimeOffset(2026, 1, 10)
            });

            context.Posts.Add(new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "og title",
                Content = "og content",
                Date = DateTimeOffset.UtcNow.AddDays(-2),
                OsmId = 123,
                LocationName = "Christchurch",
                PhotoIds = []
            });

            await context.SaveChangesAsync();
        }

        SetupGeoMock();

        var oversizedBytes = new byte[10000001];

        using var content = BuildUpdatePostForm(photos: [(oversizedBytes, "huge.png", "image/png")]);

        var response = await HttpClient.PutAsync("/api/post/1", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePost_MultipleInvalidPhotos_BadRequestAllBadFilesListed()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "owner@ex.com",
                DisplayName = "poster",
                PasswordKey = "password",
                Country = "NZ"
            });

            context.Trips.Add(new Trip
            {
                Id = 1,
                UserId = 1,
                Title = "test trip",
                Description = "test description",
                StartDate = DateToDateTimeOffset(2026, 1, 1),
                EndDate = DateToDateTimeOffset(2026, 1, 10)
            });

            context.Posts.Add(new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "og title",
                Content = "og content",
                Date = DateTimeOffset.UtcNow.AddDays(-2),
                OsmId = 123,
                LocationName = "Christchurch",
                PhotoIds = []
            });

            await context.SaveChangesAsync();
        }

        SetupGeoMock();

        var invalidBytesOne = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var invalidBytesTwo = new byte[] { 0xde, 0xad, 0xbe, 0xef };

        using var content = BuildUpdatePostForm(photos:
        [
            (invalidBytesOne, "bad1.png", "image/png"),
            (invalidBytesTwo, "bad2.jpeg", "image/jpeg")
        ]);

        var response = await HttpClient.PutAsync("/api/post/1", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = await response.Content.ReadFromJsonAsync<BadRequestValidationListResponse>();
        errors!.Errors["photos"].Count.ShouldBe(2);
        errors.Errors["photos"].ShouldContain(e => e.Contains("bad1.png"));
        errors.Errors["photos"].ShouldContain(e => e.Contains("bad2.jpeg"));
    }
}
