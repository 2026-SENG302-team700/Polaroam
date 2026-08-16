using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using Shouldly;
using Xunit.Abstractions;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class ImageControllerTest : BaseIntegrationTestFixture
{
    private ITestOutputHelper _output;

    public ImageControllerTest(WebApplicationFactory<Program> webApplicationFactory, ITestOutputHelper output) : base(
        webApplicationFactory)
    {
        _output = output;
    }
    
    private IFormFile GetMockFile(string filename, Byte[] content, string mimeType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(),
            ContentType = mimeType
        };
    }

    // Helper Functions
    private async Task SetupUserAndTrip(int userId, string email)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User { Id = userId, Email = email, DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = userId, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 8, 9, 14, 30, 0, TimeSpan.FromHours(12)) });
        context.Albums.Add(new Album { TripId = 1, PhotoIds = new() });
        await context.SaveChangesAsync();
    }
    
    [Fact]
    public async Task GetPhotoTags_ValidId_ReturnsOk()
    {
        List<string> tags = ["Berlin", "German", "Bratwurst", "Volkswagen", "Lederhosen"];
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
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.FromHours(12)), });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
            Tags = tags
        });
        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newTags = await response.Content.ReadFromJsonAsync<List<string>>();
        newTags.ShouldNotBeNull();
        foreach (var tag in newTags)
        {
            tags.ShouldContain(tag);
        }
    }
    
    private async Task SetupSecondTrip(int userId, int tripId)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Trips.Add(new Trip { Id = tripId, Title = "Second trip", UserId = userId, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 9, 8, 14, 30, 0, TimeSpan.FromHours(12)) });
        context.Albums.Add(new Album { TripId = tripId, PhotoIds = new() });
        await context.SaveChangesAsync();
    }

    private async Task AddTaggedImagesToAlbum(int tripId, params (int Id, List<string>? Tags)[] files)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var album = context.Albums.First(a => a.TripId == tripId);
        foreach (var (id, tags) in files)
        {
            album.PhotoIds.Add(id);
            context.CustomFiles.Add(new CustomFile
            {
                Id = id, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, Tags = tags
            });
        }
        await context.SaveChangesAsync();
    }

    // GetSuggestedTags Tests
    [Fact]
    public async Task GetSuggestedTags_ValidQuery_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["beach", "sunset", "mountain"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        _output.WriteLine(await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_PartialMatch_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["beachside", "sunset"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beachside"]);
    }

    [Fact]
    public async Task GetSuggestedTags_CaseInsensitive_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["Beach", "Sunset"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["Beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_NoMatchingTags_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["mountain", "forest"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTags_NoAlbums_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTags_FilesWithNullTags_AreIgnored()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, null), (2, ["beach"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_DuplicateTagsAcrossFiles_ReturnsDistinctTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1,
            (1, ["beach", "sunset"]),
            (2, ["beach", "mountain"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_AcrossMultipleTrips_ReturnsAllOwnedTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await SetupSecondTrip(1, 2);
        await AddTaggedImagesToAlbum(1, (1, ["beachfront"]));
        await AddTaggedImagesToAlbum(2, (2, ["beachball"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beachball", "beachfront"], ignoreOrder: true);
    }
    
    [Fact]
    public async Task GetSuggestedTags_OtherUsersTags_AreNotReturned()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["beach"]));

        // A trip/album/tag belonging to a different user
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Users.Add(new User { Id = 404, Email = "notMe@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });
            context.Trips.Add(new Trip { Id = 99, Title = "Other trip", UserId = 404, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 9, 8, 14, 30, 0, TimeSpan.FromHours(12)) });
            context.Albums.Add(new Album { TripId = 99, PhotoIds = [2] });
            context.CustomFiles.Add(new CustomFile { Id = 2, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 404, Tags = ["beachhouse"] });
            await context.SaveChangesAsync();
        }

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }
    
    [Fact]
    public async Task GetPhotoTags_ValidIdInQueue_ReturnsOk()
    {

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
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.FromHours(12)),});
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
            InTagQueue = true
        });
        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("Tags are being generated.");
    }
    
    [Fact]
    public async Task GetPhotoTags_ValidIdNoTags_ReturnsNoContent()
    {

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
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.FromHours(12)), });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
        });

        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

    }

    // [Fact]
    // public async Task RetryPhotoTagging_ValidId_ReturnsOK()
    // {
    //     await SetupUserAndTrip(1, "test@example.com");
    //     
    //     await using var context = await DbContextFactory.CreateDbContextAsync();
    //
    //     var mockFileContent = File.ReadAllBytes("resources/panda.webp");
    //     
    //     var mockFile = GetMockFile(
    //         "mocka",
    //         mockFileContent,
    //         "image/webp");
    //     
    //     
    // }
}