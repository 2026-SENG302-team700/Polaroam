using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using Shouldly;
using Xunit.Abstractions;

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
        context.Users.Add(new User { Id = userId, Email = email, DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = userId, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
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
            PostId = 1, TripId = 1, LocationName = "Location A", Title = "Title", OsmId = 1, Content = "Content",
            Date = new DateOnly(2025, 12, 21), PhotoIds = [1, 2]
        });
        context.CustomFiles.Add(new CustomFile { Id = 1, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, });
        context.CustomFiles.Add(new CustomFile { Id = 2, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, });
        await context.SaveChangesAsync();
    }

    private async Task AddAlbumImages()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Trips.First(trip => trip.Id == 1).AlbumPhotoIds.AddRange([3, 4]);
        context.CustomFiles.Add(new CustomFile { Id = 3, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, });
        context.CustomFiles.Add(new CustomFile { Id = 4, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 1, });
        await context.SaveChangesAsync();
    }
    
    
    // GetImagesFromAlbum Tests
    [Fact]
    public async Task GetImagesFromAlbum_AlbumImagesOnly_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1");
        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<CustomFile[]>();
        images!.Length.ShouldBe(2);
    }
    
    [Fact]
    public async Task GetImagesFromAlbum_PostImagesOnly_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddPostImages();
        var response = await HttpClient.GetAsync($"/api/album/1");
        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<CustomFile[]>();
        images!.Length.ShouldBe(2);
    }
    
    [Fact]
    public async Task GetImagesFromAlbum_PostAndAlbumImages_ReturnSuccess()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddPostImages();
        await AddAlbumImages();
        var response = await HttpClient.GetAsync($"/api/album/1");
        response.EnsureSuccessStatusCode();
        var images = await response.Content.ReadFromJsonAsync<CustomFile[]>();
        images!.Length.ShouldBe(4);
    }

    [Fact]
    public async Task GetImagesFromAlbum_NotTripOwner_ReturnUnauthorized()
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
        await using var context = DbContextFactory.CreateDbContext();
        var trip = context.Trips.FirstOrDefault(trip => trip.Id == 1)!;
        trip.AlbumPhotoIds.Count.ShouldBe(2);
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
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        await context.SaveChangesAsync();
        
        var pandaBytes = GetBytes("panda.webp");
        var kayakBytes = GetBytes("kayak.webp");
        var content = CreateUploadImagesContent(
        [
            (pandaBytes, "panda.webp", "image/webp"),
            (kayakBytes, "kayak.webp", "image/webp")
        ]);
        var response = await HttpClient.PostAsync("/api/album/1/images", content);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}