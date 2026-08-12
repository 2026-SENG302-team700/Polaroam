using System.Net;
using System.Net.Http.Headers;
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

    private async Task uploadImagesSetup(int userId, string email)
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = userId, Email = email, DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = userId, Title = "My trip", UserId = 1, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
        await context.SaveChangesAsync();
    }

    private byte[] GetBytes(string filename)
    {
        return File.ReadAllBytes($"resources/{filename}");
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
    
    [Fact]
    public async Task AddImagesToAlbum_ValidImages_ReturnSuccess()
    {
        await uploadImagesSetup(1, "test@example.com");
        
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
        await uploadImagesSetup(404, "notMe@example.com");
        
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
        await uploadImagesSetup(1, "test@example.com");
        
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