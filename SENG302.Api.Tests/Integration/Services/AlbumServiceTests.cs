using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll.Assist;
using SENG302.Api.Models.Entities;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Services;

public class AlbumServiceTests :BaseIntegrationTestFixture
{ 
    private IAlbumService ServiceUnderTest => ServiceProvider.GetRequiredService<IAlbumService>();
    public AlbumServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory) { }

    private IFormFile GetMockFile(string filename, Byte[] content, string mimeType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(),
            ContentType = mimeType
        };
    }

    [Fact]
    public async Task UploadAlbumImages_ValidImages_ReturnsAlbumWithPhotoId()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        context.Users.Add(new User
        {
            Id = 1,
            Email = $"test{1}@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "Test List",
            UserId = 1,
            Description = "Test",
            StartDate = new DateOnly(2017, 10, 08),
            EndDate = new DateOnly(2017, 10, 21)
        });
        context.Albums.Add(new TripAlbum
        {
            TripId = 1,
            PhotoIds = []
        });

        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");
        List<IFormFile> photos = [mockFile];

        var album = await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);
        album.ShouldNotBeNull();
        album.PhotoIds.Count.ShouldBe(1);
        album.PhotoIds.ShouldContain(1);
    }
}