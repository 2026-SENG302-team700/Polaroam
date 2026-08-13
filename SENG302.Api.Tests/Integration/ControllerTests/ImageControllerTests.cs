using System.Net;
using System.Net.Http.Json;
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

    // Helper Functions
    private async Task SetupUserAndTrip(int userId, string email)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User { Id = userId, Email = email, DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = userId, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
        context.Albums.Add(new Album { TripId = 1, PhotoIds = new() });
        await context.SaveChangesAsync();
    }

    private async Task SetupSecondTrip(int userId, int tripId)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Trips.Add(new Trip { Id = tripId, Title = "Second trip", UserId = userId, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
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

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
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

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beachside"]);
    }

    [Fact]
    public async Task GetSuggestedTags_CaseInsensitive_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["Beach", "Sunset"]));

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["Beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_NoMatchingTags_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["mountain", "forest"]));

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTags_NoAlbums_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTags_FilesWithNullTags_AreIgnored()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, null), (2, ["beach"]));

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
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

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
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

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
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
            context.Trips.Add(new Trip { Id = 99, Title = "Other trip", UserId = 404, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
            context.Albums.Add(new Album { TripId = 99, PhotoIds = [2] });
            context.CustomFiles.Add(new CustomFile { Id = 2, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 404, Tags = ["beachhouse"] });
            await context.SaveChangesAsync();
        }

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_UserDoesNotExist_ReturnsUnauthorized()
    {
        // No user seeded in the DB for the authenticated id
        var response = await HttpClient.GetAsync("/api/image/tags/beach");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSuggestedTags_ResultsAreLimited()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var files = Enumerable.Range(1, 15)
            .Select(i => (i, (List<string>?)[$"beach{i:D2}"]))
            .ToArray();
        await AddTaggedImagesToAlbum(1, files);

        var response = await HttpClient.GetAsync("/api/image/tags/beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags!.Length.ShouldBeLessThanOrEqualTo(5); // adjust to match MaxSuggestedTags
    }
    
    
    // GetSuggestedTagsForTrip Tests
    [Fact]
    public async Task GetSuggestedTagsForTrip_ValidQuery_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["beach", "sunset", "mountain"]));

        var response = await HttpClient.GetAsync("/api/image/tags/trip/1?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTagsForTrip_CaseInsensitive_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["Beach", "Sunset"]));

        var response = await HttpClient.GetAsync("/api/image/tags/trip/1?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["Beach"]);
    }

    [Fact]
    public async Task GetSuggestedTagsForTrip_NoMatchingTags_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["mountain", "forest"]));

        var response = await HttpClient.GetAsync("/api/image/tags/trip/1?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTagsForTrip_DuplicateTagsAcrossFiles_ReturnsDistinctTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1,
            (1, ["beach", "sunset"]),
            (2, ["beach", "mountain"]));

        var response = await HttpClient.GetAsync("/api/image/tags/trip/1?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTagsForTrip_OtherOwnedTripsExcluded_ReturnsOnlyRequestedTrip()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await SetupSecondTrip(1, 2);
        await AddTaggedImagesToAlbum(1, (1, ["beachfront"]));
        await AddTaggedImagesToAlbum(2, (2, ["beachball"]));

        var response = await HttpClient.GetAsync("/api/image/tags/trip/1?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beachfront"]);
    }

    [Fact]
    public async Task GetSuggestedTagsForTrip_TripBelongsToAnotherUser_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");

        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Users.Add(new User { Id = 404, Email = "notMe@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });
            context.Trips.Add(new Trip { Id = 99, Title = "Other trip", UserId = 404, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
            context.Albums.Add(new Album { TripId = 99, PhotoIds = [2] });
            context.CustomFiles.Add(new CustomFile { Id = 2, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 404, Tags = ["beachhouse"] });
            await context.SaveChangesAsync();
        }

        var response = await HttpClient.GetAsync("/api/image/tags/trip/99?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTagsForTrip_TripDoesNotExist_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var response = await HttpClient.GetAsync("/api/image/tags/trip/999?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTagsForTrip_UserDoesNotExist_ReturnsUnauthorized()
    {
        // No user seeded in the DB for the authenticated id
        var response = await HttpClient.GetAsync("/api/image/tags/trip/1?query=beach");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSuggestedTagsForTrip_ResultsAreLimited()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var files = Enumerable.Range(1, 15)
            .Select(i => (i, (List<string>?)[$"beach{i:D2}"]))
            .ToArray();
        await AddTaggedImagesToAlbum(1, files);

        var response = await HttpClient.GetAsync("/api/image/tags/trip/1?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags!.Length.ShouldBeLessThanOrEqualTo(10); // adjust to match MaxSuggestedTags
    }
}