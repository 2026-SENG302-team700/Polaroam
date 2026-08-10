using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
            mockFileContent1,
            "image/png");
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

    [Fact]
    public async Task UploadImageAlbumImages_InvalidImageSize_ThrowsMultipleValidationListException()
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

        var file = GetMockFile("test", new byte[10000001] , "image/png");

        List<IFormFile> photos = [file];

        MultipleValidationListException ex = await Should.ThrowAsync<MultipleValidationListException>(async () =>
        {
            await ServiceUnderTest.UploadImagesToAlbumAsync(photos,1);
        });
        ex.Errors["photos"].ShouldContain("test: Image to large, maximum file size is 10MB");
    }
    
    [Fact]
    public async Task UploadImageAlbumImages_InvalidMimeType_ThrowsMultipleValidationListException()
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

        var file = GetMockFile("test", new byte[100000] ,"application/octet-stream");

        List<IFormFile> photos = [file];

        MultipleValidationListException ex = await Should.ThrowAsync<MultipleValidationListException>(async () =>
        {
            await ServiceUnderTest.UploadImagesToAlbumAsync(photos,1);
        });
        ex.Errors["photos"].ShouldContain("test: Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp");
    }
    
    [Fact]
    public async Task DeleteImagesFromAlbum_ValidPhotoIdValidTripId_DeletesImage()
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
        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");
        List<IFormFile> photos = [mockFile];
        
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        
        await context.SaveChangesAsync();
        
        await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);

        await Should.NotThrowAsync(async () =>
        {
            await ServiceUnderTest.DeleteImageFromAlbumAsync(1,1);
        });
    }


    [Fact]
    public async Task DeleteImagesFromAlbum_AlbumDoesNotExist_KeyNotFoundExceptionThrown()
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

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.DeleteImageFromAlbumAsync(1,1);
        });
        ex.Message.ShouldContain("Album with that Id does not exist.");
    }

    [Fact]
    public async Task DeleteImagesFromAlbum_FileDoesNotExist_FileNotFoundExceptionThrown()
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
            PhotoIds = [1, 2]
        });
        
        await context.SaveChangesAsync();

        FileNotFoundException ex = await Should.ThrowAsync<FileNotFoundException>(async () =>
        {
            await ServiceUnderTest.DeleteImageFromAlbumAsync(1,1);
        });
        
        ex.Message.ShouldContain("File 1 not found");
    }
    
    [Fact]
    public async Task DeleteImagesFromAlbum_PhotoDoesNotExistOnAlbum_KeyNotFoundExceptionThrown()
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
        context.Trips.Add(new Trip
        {
            Id = 2,
            Title = "Test List 2",
            UserId = 1,
            Description = "Test2",
            StartDate = new DateOnly(2017, 10, 08),
            EndDate = new DateOnly(2017, 10, 21)
        });

        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        context.Albums.Add(new Album()
        {
            TripId = 2,
            PhotoIds = []
        });
        
        await context.SaveChangesAsync();
        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");
        List<IFormFile> photos = [mockFile];
        

        await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 2);
        

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.DeleteImageFromAlbumAsync(1,1);
        });
        
        ex.Message.ShouldContain("Photo with that Id does not exist in this album.");
    }

    [Fact]
    public async Task GetImagesFromAlbum_ValidTripId_GetCustomFiles()
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
        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");
        List<IFormFile> photos = [mockFile];
        
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        
        await context.SaveChangesAsync();
        
        await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);
        
        await Should.NotThrowAsync(async () =>
        {
            await ServiceUnderTest.GetImagesFromAlbumAsync(1);
        });
    }
    
    [Fact]
    public async Task GetImagesFromAlbum_NoAlbum_ThrowsKeyNotFoundException()
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
        
        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.GetImagesFromAlbumAsync(1);
        });
        
        ex.Message.ShouldContain("Album with that Id does not exist.");
    }
    
    [Fact]
    public async Task GetImagesFromAlbum_InvalidTripId_ThrowsKeyNotFoundException()
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
        
        
        await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);
        
        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.GetImagesFromAlbumAsync(2);
        });
        
        ex.Message.ShouldContain("Trip with that Id does not exist.");
    }
}