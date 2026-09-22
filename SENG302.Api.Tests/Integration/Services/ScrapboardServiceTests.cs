using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.Core;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.Services;

public class ScrapboardServiceTests : BaseIntegrationTestFixture
{
    private IScrapboardService ServiceUnderTest => ServiceProvider.GetRequiredService<IScrapboardService>();
    private readonly ITripService _tripService;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IUserService _mockUserService;
    public ScrapboardServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory)
    {
        _mockUserService = Substitute.For<IUserService>();
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _tripService = new TripService(DbContextFactory, _fakeTimeProvider, _mockUserService);
    }

    private async Task SetupUserAndTrip(int userId, int tripId)
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = userId,
            Email = $"test{userId}@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip
        {
            Id = tripId,
            Title = "Test List",
            UserId = userId,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Albums.Add(new Album
        {
            TripId = tripId,
            PhotoIds = [],
        });
        await context.SaveChangesAsync();
    }

    private IFormFile GetMockFile(string filename, byte[] content, string mimeType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(),
            ContentType = mimeType
        };
    }

    [Fact]
    public async Task CreateScrapboard_TripHasNoScrapboard_Success()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();
        var scrapboard = ServiceUnderTest.CreateScrapboardAsync(1);
        var newScrapboard = await context.Scrapboards.FirstOrDefaultAsync(s => s.TripId == 1);
        var trip = await _tripService.GetTripByIdAsync(1);
        await context.SaveChangesAsync();
        trip.HasScrapboard.ShouldBeTrue();
        newScrapboard.ShouldNotBeNull();
        newScrapboard.TripId.Equals(1);
        scrapboard.Equals(newScrapboard);
    }

    [Fact]
    public async Task CreateScrapboard_TripHasScrapboard_Failure()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();
        var scrapboard = ServiceUnderTest.CreateScrapboardAsync(1);
        var trip = await _tripService.GetTripByIdAsync(1);
        trip.HasScrapboard.ShouldBeTrue();
        var exception =
            await Should.ThrowAsync<ArgumentException>(
                () => ServiceUnderTest.CreateScrapboardAsync(1)
            );
        exception.Message.ShouldContain("This trip already has a Scrapboard.");
    }

    [Fact]
    public async Task GetScrapboardByTrip_TripHasScrapboard_Success()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();
        
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1,
        });
        await context.SaveChangesAsync();
        var scrapboard = await ServiceUnderTest.GetScrapboardByTripAsync(1);
        scrapboard.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetScrapboardByTrip_TripHasNoScrapboard_ReturnsNull()
    {
        await SetupUserAndTrip(1,1);
        var scrapboard = await ServiceUnderTest.GetScrapboardByTripAsync(1);
        scrapboard.ShouldBeNull();
    }

    [Fact]
    public async Task GetScrapboardThumbnail_HasThumbnail_ReturnsFile()
    {
        await SetupUserAndTrip(1, 1);
        await using var context = DbContextFactory.CreateDbContext();

        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");
        var mockFile = GetMockFile("thumb.webp", mockFileContent, "image/webp");
        var savedFile = await fileService.SaveFileAsync(mockFile, 1);

        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1,
            Thumbnail = savedFile.Id
        });
        await context.SaveChangesAsync();

        var thumbnail = await ServiceUnderTest.GetScrapboardThumbnailAsync(1);

        thumbnail.Id.ShouldBe(savedFile.Id); 
    }

    [Fact]
    public async Task GetScrapboardThumbnail_NoScrapboard_KeyNotFoundException()
    {
        await SetupUserAndTrip(1, 1);

        await Should.ThrowAsync<KeyNotFoundException>(() =>
        {
            return ServiceUnderTest.GetScrapboardThumbnailAsync(1);
        });
    }

    [Fact]
    public async Task GetScrapboardThumbnail_NoThumbnail_FileNotFoundException()
    {
        await SetupUserAndTrip(1, 1);
        await using var context = DbContextFactory.CreateDbContext();
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1
        });
        await context.SaveChangesAsync();

        await Should.ThrowAsync<FileNotFoundException>(() =>
        {
            return ServiceUnderTest.GetScrapboardThumbnailAsync(1);
        });
    }

    [Fact]
    public async Task SaveScrapboard_Valid_UpdatesScrapboard()
    {
        await SetupUserAndTrip(1, 1);
        await using var context = DbContextFactory.CreateDbContext();
        context.Scrapboards.Add(new Scrapboard { TripId = 1 });
        await context.SaveChangesAsync();

        var fileContent = File.ReadAllBytes("resources/panda.webp");
        var files = new List<IFormFile> { GetMockFile("element1.webp", fileContent, "image/webp") };
        var thumbnail = GetMockFile("thumb.webp", fileContent, "image/webp");

        await ServiceUnderTest.SaveScrapboardAsync(1, "[]", files, thumbnail);

        await using var assertContext = await DbContextFactory.CreateDbContextAsync();
        var updatedScrapboard = await assertContext.Scrapboards.FirstOrDefaultAsync(s => s.TripId == 1);
        updatedScrapboard!.Elements.ShouldBe("[]");
        updatedScrapboard.Files.ShouldHaveSingleItem();
        updatedScrapboard.Thumbnail.ShouldNotBe(-1);
    }

    [Fact]
    public async Task SaveScrapboard_NoScrapboard_KeyNotFoundException()
    {
        await SetupUserAndTrip(1, 1);

        var fileContent = File.ReadAllBytes("resources/panda.webp");
        var thumbnail = GetMockFile("thumb.webp", fileContent, "image/webp");

        await Should.ThrowAsync<KeyNotFoundException>(() =>
        {
            return ServiceUnderTest.SaveScrapboardAsync(1, "[]", new List<IFormFile>(), thumbnail);
        });
    }

    [Fact]
    public async Task SaveScrapboard_ThumbnailTooLarge_FileSizeExceededException()
    {
        await SetupUserAndTrip(1, 1);
        await using var context = DbContextFactory.CreateDbContext();
        context.Scrapboards.Add(new Scrapboard { TripId = 1 });
        await context.SaveChangesAsync();

        var thumbnail = GetMockFile("thumb.webp", new byte[10_000_001], "image/webp");

        await Should.ThrowAsync<FileSizeExceededException>(
            () =>
            {
                return ServiceUnderTest.SaveScrapboardAsync(1, "[]", new List<IFormFile>(), thumbnail);
            });
    }

    [Fact]
    public async Task SaveScrapboard_ThumbnailInvalidMimeType_FileMimeTypeInvalidException()
    {
        await SetupUserAndTrip(1, 1);
        await using var context = DbContextFactory.CreateDbContext();
        context.Scrapboards.Add(new Scrapboard { TripId = 1 });
        await context.SaveChangesAsync();

        var thumbnail = GetMockFile("thumb.txt", [0, 0, 0, 1], "image/webp");

        await Should.ThrowAsync<FileMimeTypeInvalidException>(
            () =>
            {
                return ServiceUnderTest.SaveScrapboardAsync(1, "[]", new List<IFormFile>(), thumbnail);
            });
    }
}