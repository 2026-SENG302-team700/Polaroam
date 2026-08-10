using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task UploadAlbumImages_SingleValidImage_ReturnsAlbumWithPhotoId()
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
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        await context.SaveChangesAsync();
        
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

    [Fact]
    public async Task UploadAlbumImages_MultipleValidImage_ReturnsAlbumWithPhotoId()
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
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        await context.SaveChangesAsync();
        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");
        
        var mockFileContent1 = File.ReadAllBytes("resources/render.png");

        var mockFile1 = GetMockFile(
            "render",
            mockFileContent,
            "render/png");
        List<IFormFile> photos = [mockFile, mockFile1];

        var album = await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);
        album.ShouldNotBeNull();
        album.PhotoIds.Count.ShouldBe(2);
        album.PhotoIds.ShouldContain(1);
        album.PhotoIds.ShouldContain(2);
    }
    [Fact]
    public async Task UploadAlbumImages_NonExistentAlbum_ThrowsKeyNotFoundException()
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
        await context.SaveChangesAsync();
        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");
        List<IFormFile> photos = [mockFile];

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.UploadImagesToAlbumAsync(photos,1);
           
        });
        ex.Message.ShouldContain("Album with that Id does not exist.");
    }
    [Fact]
    public async Task UploadAlbumImages_NonExistentTrip_ThrowsKeyNotFoundException()
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
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        
        await context.SaveChangesAsync();
        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");
        List<IFormFile> photos = [mockFile];

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.UploadImagesToAlbumAsync(photos,1);
           
        });
        ex.Message.ShouldContain("Trip does not exist");
    }

    // [Theory]
    // [InlineData(10000001)]

    // public async Task UploadImageAlbumImages_InvalidImageSize_ThrowsMultipleValidationListException(Int64 fileSize)
    // {
    //     await using var context = await DbContextFactory.CreateDbContextAsync();

    //     context.Users.Add(new User
    //     {
    //         Id = 1,
    //         Email = $"test{1}@test.com",
    //         DisplayName = "Test User",
    //         PasswordKey = "password",
    //         Country = "Test Country"
    //     });
    //     context.Trips.Add(new Trip
    //     {
    //         Id = 1,
    //         Title = "Test List",
    //         UserId = 1,
    //         Description = "Test",
    //         StartDate = new DateOnly(2017, 10, 08),
    //         EndDate = new DateOnly(2017, 10, 21)
    //     });
    //     context.Albums.Add(new Album
    //     {
    //         TripId = 1,
    //         PhotoIds = []
    //     });
    //     await context.SaveChangesAsync();

    //     var file = GetMockFile("test", new byte[fileSize] , "image/png");

    //     List<IFormFile> photos = [file];

    //     MultipleValidationListException ex = await Should.ThrowAsync<MultipleValidationListException>(async () =>
    //     {
    //         await ServiceUnderTest.UploadImagesToAlbumAsync(photos,1);
    //     });
    //     ex.Errors["photos"].ShouldContain("test: Image to large, maximum file size is 10MB");
    // }
}