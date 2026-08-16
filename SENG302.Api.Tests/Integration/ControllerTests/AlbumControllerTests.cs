using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using Shouldly;
using Xunit.Abstractions;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class AlbumControllerTest : BaseIntegrationTestFixture
{
    private readonly ITestOutputHelper _output;
    private IFileService FileServiceMock => ServiceProvider.GetRequiredService<IFileService>();
    

    public AlbumControllerTest(WebApplicationFactory<Program> webApplicationFactory, ITestOutputHelper output) : base(
        webApplicationFactory)
    {
        _output = output;
    }
    
    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        services.RemoveAll<IFileService>();
        services.AddSingleton(Substitute.For<IFileService>());
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
        var file1 = new CustomFile { Id = 1, FileKey = "key1", MimeType = "image/webp", OriginalFileName = "file1.webp", OwnerId = 1 };
        var file2 = new CustomFile { Id = 2, FileKey = "key2", MimeType = "image/webp", OriginalFileName = "file2.webp", OwnerId = 1 };
        context.CustomFiles.Add(file1);
        context.CustomFiles.Add(file2);
        await context.SaveChangesAsync();
        FileServiceMock.GetFileByIdAsync(1).Returns(file1);
        FileServiceMock.GetFileByIdAsync(2).Returns(file2);
    }

    private async Task AddAlbumImages()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Albums.First(album => album.TripId == 1).PhotoIds.AddRange(3, 4);
        var file3 = new CustomFile { Id = 3, FileKey = "key3", MimeType = "image/webp", OriginalFileName = "file3.webp", OwnerId = 1 };
        var file4 = new CustomFile { Id = 4, FileKey = "key4", MimeType = "image/webp", OriginalFileName = "file4.webp", OwnerId = 1 };
        context.CustomFiles.Add(file3);
        context.CustomFiles.Add(file4);
        await context.SaveChangesAsync();
        FileServiceMock.GetFileByIdAsync(3).Returns(file3);
        FileServiceMock.GetFileByIdAsync(4).Returns(file4);
    }

    // GetAlbumImage Tests
    [Fact]
    public async Task GetAlbumImage_ValidImage_ReturnSuccess()
    {
        FileServiceMock.GetFileContentAsync(Arg.Any<string>()).Returns([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1/images/3");
        _output.WriteLine(await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAlbumImage_PostImage_ReturnSuccess()
    {
        FileServiceMock.GetFileContentAsync(Arg.Any<string>()).Returns([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
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
        FileServiceMock.GetFileByIdAsync(Arg.Any<int>()).Throws<KeyNotFoundException>();
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
        FileServiceMock.SaveFileAsync(Arg.Any<FormFile>(), Arg.Any<int>()).Returns(new CustomFile { Id = 1, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, });
        await SetupUserAndTrip(1, "test@example.com");

        var pandaBytes = GetBytes("panda.webp");
        var kayakBytes = GetBytes("kayak.webp");
        var content = CreateUploadImagesContent(
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (kayakBytes, "kayak.webp", "image/webp")
        ]);

        var response = await HttpClient.PostAsync("/api/album/1/images", content);
        _output.WriteLine(await response.Content.ReadAsStringAsync());
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
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Albums.First(album => album.TripId == 1).PhotoIds.ShouldNotContain(3);
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