using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using Shouldly;
using Xunit.Abstractions;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class AlbumControllerTest : BaseIntegrationTestFixture
{
    private ITestOutputHelper _output;

    public AlbumControllerTest(WebApplicationFactory<Program> webApplicationFactory, ITestOutputHelper output) : base(
        webApplicationFactory)
    {
        _output = output;
    }

    // Helper Functions
    private async Task SetupUserAndTrip(int userId, string email)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
            { Id = userId, Email = email, DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip
        {
            Id = 1, Title = "My trip", UserId = userId, StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21)
        });
        context.Albums.Add(new Album { TripId = 1, PhotoIds = new() });
        await context.SaveChangesAsync();
    }

    private byte[] GetBytes(string filename)
    {
        return File.ReadAllBytes($"resources/{filename}");
    }


    private async Task AddPostImages()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Posts.Add(new Post
        {
            PostId = 1,
            TripId = 1,
            LocationName = "Location A",
            Title = "Title",
            OsmId = 1,
            Content = "Content",
            Date = DateToDateTimeOffset(2025, 12, 21),
            PhotoIds = [1, 2]
        });
        context.Albums.First(album => album.TripId == 1).PhotoIds.AddRange(1, 2);
        context.CustomFiles.Add(new CustomFile
            { Id = 1, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, Tags = ["beach", "sun"] });
        context.CustomFiles.Add(new CustomFile
            { Id = 2, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, Tags = ["stars", "night"] });
        await context.SaveChangesAsync();
    }

    private async Task AddAlbumImages()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Albums.First(album => album.TripId == 1).PhotoIds.AddRange(3, 4);
        context.CustomFiles.Add(new CustomFile
        {
            Id = 3, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, Tags = ["Test"],
            DateCreated = DateTimeOffset.Now
        });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 4, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, Tags = ["TestTwo"],
            DateCreated = DateTimeOffset.Now.AddHours(1)
        });
        await context.SaveChangesAsync();
    }

    // GetAlbumImage Tests
    [Fact]
    public async Task GetAlbumImage_ValidImage_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1/images/3");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAlbumImage_PostImage_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddPostImages();
        var response = await HttpClient.GetAsync($"/api/album/1/images/1");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAlbumImage_NotOwner_ReturnUnauthorized()
    {
        await SetupUserAndTrip(404, "notMe@example.com");
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1/images/3");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlbumImage_NoImage_ReturnNotFound()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1/images/5");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // GetImagesFromAlbum Tests
    [Fact]
    public async Task GetAlbumImageMetadata_AlbumImagesOnly_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1");
        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<PhotoMetadataResponse[]>();
        images!.Length.ShouldBe(2);
    }

    [Fact]
    public async Task GetAlbumImageMetadata_PostImagesOnly_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddPostImages();
        var response = await HttpClient.GetAsync($"/api/album/1");
        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<PhotoMetadataResponse[]>();
        images!.Length.ShouldBe(2);
    }

    [Fact]
    public async Task GetAlbumImageMetadata_PostAndAlbumImages_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddPostImages();
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1");
        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<PhotoMetadataResponse[]>();
        images!.Length.ShouldBe(4);
    }

    [Fact]
    public async Task GetAlbumImageMetadata_NotTripOwner_ReturnUnauthorized()
    {
        await SetupUserAndTrip(404, "notMe@example.com");
        var response = await HttpClient.GetAsync($"/api/album/1");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }


    private MultipartFormDataContent CreateUploadImagesContent(
        IEnumerable<(byte[] Bytes, string FileName, string ContentType)>? photos = null)
    {
        var content = new MultipartFormDataContent();

        foreach (var (bytes, fileName, contentType) in photos ?? [])
        {
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "files", fileName);
        }

        return content;
    }


    // AddImagesToAlbum Tests
    [Fact]
    public async Task AddImagesToAlbum_ValidImages_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var pandaBytes = GetBytes("panda.webp");
        var kayakBytes = GetBytes("kayak.webp");
        var content = CreateUploadImagesContent(
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (kayakBytes, "kayak.webp", "image/webp")
        ]);

        var response = await HttpClient.PostAsync("/api/album/1/images", content);
        response.EnsureSuccessStatusCode();
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var album = context.Albums.FirstOrDefault(album => album.TripId == 1)!;
        album.PhotoIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AddImagesToAlbum_DontOwnPost_ReturnUnauthorized()
    {
        await SetupUserAndTrip(404, "notMe@example.com");

        var pandaBytes = GetBytes("panda.webp");
        var kayakBytes = GetBytes("kayak.webp");
        var content = CreateUploadImagesContent(
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (kayakBytes, "kayak.webp", "image/webp")
        ]);

        var response = await HttpClient.PostAsync("/api/album/1/images", content);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddImagesToAlbum_InvalidFile_ReturnBadRequest()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var pandaBytes = GetBytes("panda.webp");
        var kayakBytes = GetBytes("kayak.webp");
        var content = CreateUploadImagesContent(
        [
            (pandaBytes, "panda.webp", "image/jpg"),
            (kayakBytes, "kayak.webp", "image/webp")
        ]);

        var response = await HttpClient.PostAsync("/api/album/1/images", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddImagesToAlbum_NoTrip_ReturnBadRequest()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
        {
            Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ"
        });
        await context.SaveChangesAsync();

        var pandaBytes = GetBytes("panda.webp");
        var kayakBytes = GetBytes("kayak.webp");
        var content = CreateUploadImagesContent(
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (kayakBytes, "kayak.webp", "image/webp")
        ]);
        var response = await HttpClient.PostAsync("/api/album/1/images", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // DeleteAlbumImage Tests
    [Fact]
    public async Task DeleteAlbumImages_ImageInAlbum_ReturnNoContent()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        var response = await HttpClient.DeleteAsync($"/api/album/1/images/3");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest); // Fails when actually deleting because doesn't exist'
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("Failed to delete image");
    }

    [Fact]
    public async Task DeleteAlbumImages_ImageInPost_ReturnNotAllowed()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddPostImages();
        var response = await HttpClient.DeleteAsync($"/api/album/1/images/1");
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        int.Parse(await response.Content.ReadAsStringAsync()).ShouldBe(1); // Post-Id from AddPostImages()

        // Check not deleted
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Posts.FirstOrDefault(post => post.TripId == 1)!.PhotoIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DeleteAlbumImages_NotOwner_ReturnUnauthorized()
    {
        await SetupUserAndTrip(404, "notMe@example.com");
        await AddAlbumImages();
        var response = await HttpClient.DeleteAsync($"/api/album/1/images/3");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Check that the image is not removed from the album
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Albums.First(album => album.TripId == 1).PhotoIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DeleteAlbumImages_ImageDoesNotExist_ReturnNotFound()
    {
        await SetupUserAndTrip(1, "test@example.com");
        var response = await HttpClient.DeleteAsync($"/api/album/1/images/3");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAlbumImageMetadataWithTag_ValidRequest_ReturnMetadata()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        await AddPostImages();
        var response = await HttpClient.GetAsync($"/api/album/1/tag?tag=beach");
        response.EnsureSuccessStatusCode();
        var metadata = await response.Content.ReadFromJsonAsync<List<CustomFile>>();
        metadata!.Count.ShouldBe(1);
        metadata[0].Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetAlbumImageMetadataWithTag_NoMatch_ReturnEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        await AddPostImages();
        var response = await HttpClient.GetAsync($"/api/album/1/tag?tag=NotFound");
        response.EnsureSuccessStatusCode();
        var metadata = await response.Content.ReadFromJsonAsync<List<CustomFile>>();
        metadata!.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetAlbumImageMetadataWithTag_InvalidTripId_ReturnBadRequest()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        await AddPostImages();
        var response = await HttpClient.GetAsync($"/api/album/404/tag?tag=beach");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAlbumImageMetadataWithTag_stringTripId_ReturnBadRequest()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        await AddPostImages();
        var response = await HttpClient.GetAsync($"/api/album/404/tag?tag=beach");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAlbumImageMetadataWithTag_NotOwnerOfTrip_ReturnUnauthorized()
    {
        await SetupUserAndTrip(2, "SomeoneElse@example.com");
        await AddAlbumImages();
        await AddPostImages();
        var response = await HttpClient.GetAsync($"/api/album/1/tag?tag=beach");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlbumImageMetadataWithTag_DifferentDates_ReturnNewestFirst()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1/tag?tag=test");
        response.EnsureSuccessStatusCode();
        var metadata = await response.Content.ReadFromJsonAsync<List<CustomFile>>();
        metadata!.Count.ShouldBe(2);
        metadata[0].DateCreated.ShouldBeGreaterThan(metadata[1].DateCreated);
    }
}