using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class SearchControllerTests : BaseIntegrationTestFixture
{
    public SearchControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory) { }

    private async Task SetupUser(int userId, string email)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
        {
            Id = userId, 
            Email = email, 
            DisplayName = "user", 
            PasswordKey = "P4$$word", 
            Country = "NZ"
        });
        await context.SaveChangesAsync();
    }

    private async Task SetupTrip(
        int tripId, 
        int userId, 
        string title, 
        string description)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Trips.Add(new Trip
        {
            Id = tripId, 
            Title = title, 
            UserId = userId, 
            Description = description,
            StartDate = new DateTimeOffset(2025, 11, 21, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2025, 12, 21, 0, 0, 0, TimeSpan.Zero)
        });
        
        context.Albums.Add(new Album
        {
            TripId = tripId, 
            PhotoIds = []
        });
        
        await context.SaveChangesAsync();
    }

    private async Task SetupPhotoInAlbum(
        int tripId, 
        int photoId, 
        List<string>? tags, 
        int ownerId
        )
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var album = context.Albums.First(a => a.TripId == tripId);
        album.PhotoIds.Add(photoId);
        context.CustomFiles.Add(new CustomFile
        {
            Id = photoId, 
            FileKey = "", 
            MimeType = "image/jpeg", 
            OriginalFileName = "mocka.jpg", 
            OwnerId = ownerId, 
            Tags = tags
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchMyTrips_TitleMatch_ReturnsTrip()
    {
        await SetupUser(1, "test@example.com");
        await SetupTrip(1, 1, "auckland", "auckland");

        var response = await HttpClient.GetAsync("/api/search/my-trips?query=auckland");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var trips = await response.Content.ReadFromJsonAsync<List<Trip>>();
        trips.Select(t => t.Id).ShouldBe([1]);
    }

    [Fact]
    public async Task SearchMyTrips_DescriptionMatch_ReturnsTrip()
    {
        await SetupUser(1, "test@example.com");
        await SetupTrip(1, 1, "going back", "i've been forced to return to wellington once again.");

        var response = await HttpClient.GetAsync("/api/search/my-trips?query=wellington");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var trips = await response.Content.ReadFromJsonAsync<List<Trip>>();
        trips.Select(t => t.Id).ShouldBe([1]);
    }

    [Fact]
    public async Task SearchMyTrips_NoMatch_ReturnsNotFound()
    {
        await SetupUser(1, "test@example.com");
        await SetupTrip(1, 1, "auckland", "auckland");

        var response = await HttpClient.GetAsync("/api/search/my-trips?query=wellington");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchMyTrips_MultipleTrips_ReturnsTrips()
    {
        await SetupUser(1, "test@example.com");
        await SetupTrip(1, 1, "auckland", "");
        await SetupTrip(2, 1, "auckland 2: electric boogaloo", "");

        var response = await HttpClient.GetAsync("/api/search/my-trips?query=auckland");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var trips = await response.Content.ReadFromJsonAsync<List<Trip>>();
        trips.Select(t => t.Id).ShouldBe([1, 2], ignoreOrder: true);
    }

    [Fact]
    public async Task SearchMyTrips_TripOwnedByAnotherUser_IsNotReturned()
    {
        await SetupUser(1, "test@example.com");
        await SetupUser(2, "test2electricboogaloo@example.com");
        await SetupTrip(1, 2, "Auckland Adventure", "");

        var response = await HttpClient.GetAsync("/api/search/my-trips?query=auckland");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchPhotos_TagMatches_ReturnsPhotoGroupedByTrip()
    {
        await SetupUser(1, "test@example.com");
        await SetupTrip(1, 1, "new brighton <3", "");
        await SetupPhotoInAlbum(1, 1, ["beach", "sunset"], 1);

        var response = await HttpClient.GetAsync("/api/search/photos?query=beach");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var groups = await response.Content.ReadFromJsonAsync<List<TripPhotoGroupResponse>>();
        groups.Count.ShouldBe(1);
        groups[0].TripId.ShouldBe(1);
        groups[0].TripTitle.ShouldBe("new brighton <3");
        groups[0].Photos.Select(p => p.PhotoId).ShouldBe([1]);
    }

    [Fact]
    public async Task SearchPhotos_PartialTagMatch_ReturnsPhoto()
    {
        await SetupUser(1, "test@example.com");
        await SetupTrip(1, 1, "new brighton <3", "");
        await SetupPhotoInAlbum(1, 1, ["beachside"], 1);

        var response = await HttpClient.GetAsync("/api/search/photos?query=beach");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var groups = await response.Content.ReadFromJsonAsync<List<TripPhotoGroupResponse>>();
        groups!.Single().Photos.Select(p => p.PhotoId).ShouldBe([1]);
    }

    [Fact]
    public async Task SearchPhotos_NoMatchingTags_ReturnsNotFound()
    {
        await SetupUser(1, "test@example.com");
        await SetupTrip(1, 1, "arthur pass", "");
        await SetupPhotoInAlbum(1, 1, ["mountain"], 1);

        var response = await HttpClient.GetAsync("/api/search/photos?query=beach");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}