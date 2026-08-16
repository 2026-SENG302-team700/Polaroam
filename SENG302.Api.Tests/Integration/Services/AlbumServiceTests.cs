using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using Shouldly;

using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.Services;

public class AlbumServiceTests : BaseIntegrationTestFixture
{
    private IPostService _postService => ServiceProvider.GetRequiredService<IPostService>();
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

    private void SetupGeoMock()
    {
        GeoServiceMock.GetLocationAsync(Arg.Any<long>()).Returns(new GeoLocationSearchResult
        {
            OsmId = 1,
            Latitude = "1",
            Longitude = "1",
            DisplayName = "Auckland",
            Name = "Auckland, New Zealand"
        });
    }

    [Fact]
    public async Task GetAlbumById_ValidId_ReturnsAlbum()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        await context.SaveChangesAsync();
        var album = await ServiceUnderTest.GetAlbumByIdAsync(1);
        album.ShouldNotBeNull();
        album.PhotoIds.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetAlbumById_InvalidId_ThrowsKeyNotFoundError()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        await context.SaveChangesAsync();
        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.GetAlbumByIdAsync(2);

        });
        ex.Message.ShouldContain("Album with that Id does not exist.");
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);

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
            await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);

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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        await context.SaveChangesAsync();

        var file = GetMockFile("test", new byte[10000001], "image/png");

        List<IFormFile> photos = [file];

        MultipleValidationListException ex = await Should.ThrowAsync<MultipleValidationListException>(async () =>
        {
            await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });
        await context.SaveChangesAsync();

        var file = GetMockFile("test", new byte[100000], "application/octet-stream");

        List<IFormFile> photos = [file];

        MultipleValidationListException ex = await Should.ThrowAsync<MultipleValidationListException>(async () =>
        {
            await ServiceUnderTest.UploadImagesToAlbumAsync(photos, 1);
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            await ServiceUnderTest.DeleteImageFromAlbumAsync(1, 1);
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });

        await context.SaveChangesAsync();

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.DeleteImageFromAlbumAsync(1, 1);
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });

        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [1, 2]
        });

        await context.SaveChangesAsync();

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.DeleteImageFromAlbumAsync(1, 1);
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Trips.Add(new Trip
        {
            Id = 2,
            Title = "Test List 2",
            UserId = 1,
            Description = "Test2",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            await ServiceUnderTest.DeleteImageFromAlbumAsync(1, 1);
        });

        ex.Message.ShouldContain("Photo with that Id does not exist in this album.");
    }

    [Fact]
    public async Task DeleteImageFromAlbum_PhotoBelongsToAPost_ExceptionThrown()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test1@test.com",
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = [],
        });
        await context.SaveChangesAsync();

        SetupGeoMock();

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "test post",
            Content = "description",
            OsmId = 1,
            Date = DateTimeOffset.UtcNow.AddDays(-2)
        };

        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var image = GetMockFile(
            "test.png",
            pngBytes,
            "image/png"
        );

        var post = await _postService.CreateNewPostAsync(
            request,
            1,
            [image]
        );


        var postId = await ServiceUnderTest.DeleteImageFromAlbumAsync(1, 1);
        postId.ShouldNotBeNull();
        postId.ShouldBe(post.PostId);
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            await ServiceUnderTest.GetImagesMetadataFromAlbumAsync(1);
        });
    }

    [Fact]
    public async Task GetSingleImageFromAlbum_ValidTripId_GetCustomFile()
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            await ServiceUnderTest.GetSingleImageMetadataFromAlbumAsync(1, 1);
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });

        await context.SaveChangesAsync();

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.GetImagesMetadataFromAlbumAsync(1);
        });

        ex.Message.ShouldContain("Album with that Id does not exist.");
    }

    [Fact]
    public async Task GetSingleImageFromAlbum_NoAlbum_ThrowsKeyNotFoundException()
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });

        await context.SaveChangesAsync();

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.GetSingleImageMetadataFromAlbumAsync(1, 1);
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            await ServiceUnderTest.GetImagesMetadataFromAlbumAsync(2);
        });

        ex.Message.ShouldContain("Trip with that Id does not exist.");
    }

    [Fact]
    public async Task GetSingleImageFromAlbum_InvalidTripId_ThrowsKeyNotFoundException()
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
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
            await ServiceUnderTest.GetSingleImageMetadataFromAlbumAsync(2, 1);
        });

        ex.Message.ShouldContain("Trip with that Id does not exist.");
    }

    [Fact]
    public async Task CreateNewAlbum_ValidTripId_AlbumCreated()
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
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        await context.SaveChangesAsync();

        await Should.NotThrowAsync(async () =>
        {
            await ServiceUnderTest.CreateNewAlbumAsync(1);
        });

        var album = await context.Set<Album>().Where(album => album.TripId == 1).FirstOrDefaultAsync();

        album.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateNewAlbum_InvalidTripId_KeyNotFoundExceptionThrown()
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
        await context.SaveChangesAsync();

        KeyNotFoundException ex = await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.CreateNewAlbumAsync(2);
        });
        ex.Message.ShouldBe("Trip with that Id does not exist.");

        var album = await context.Set<Album>().Where(album => album.TripId == 2).FirstOrDefaultAsync();

        album.ShouldBeNull();
    }


}