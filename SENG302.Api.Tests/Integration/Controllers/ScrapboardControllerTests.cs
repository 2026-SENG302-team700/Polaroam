using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class ScrapboardControllerTests :BaseIntegrationTestFixture
{

    public ScrapboardControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory){}

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
        context.Users.Add(new User
        {
            Id = userId+1,
            Email = $"test{userId+1}@test.com",
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

    [Fact]
    public async Task GetScrapboard_ValidData_Ok()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();
        
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1,
        });
        await context.SaveChangesAsync();

        var message = await HttpClient.GetAsync("/api/scrapboard/1");
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetScrapboard_BoardDoesNotExist_NotFound()
    {
        await SetupUserAndTrip(1,1);
        var message = await HttpClient.GetAsync("/api/scrapboard/1");
        message.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetScrapboard_TripDoesNotExist_BadRequest()
    {
        await SetupUserAndTrip(1,1);
        var message = await HttpClient.GetAsync("/api/scrapboard/4");
        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetScrapboard_NotPermittedToAccessBoard_Unauthorized()
    {
        int userId = 1;
        int tripId = 1;
        await SetupUserAndTrip(userId,tripId);

        await using var context = DbContextFactory.CreateDbContext();
        
        context.Trips.Add(new Trip
        {
            Id = tripId+1,
            Title = "Test List",
            UserId = userId+1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = tripId+1,
        });
        await context.SaveChangesAsync();

        var message = await HttpClient.GetAsync("/api/scrapboard/2");
        message.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetScrapboard_WithFiles_ReturnOk()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();

        const string fileKey = "a";

        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = fileKey,
            OriginalFileName = "filename.jpeg",
            MimeType = "image/jpeg",
        });
        
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1,
            Files = [1]
        });
        await context.SaveChangesAsync();
        
        var filePath = Path.Combine(FakeTestDirectory, fileKey);
        await File.WriteAllBytesAsync(filePath, [1, 2, 3, 4]);

        var message = await HttpClient.GetAsync("/api/scrapboard/1");
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateScrapboard_ValidData_Ok()
    {
        await SetupUserAndTrip(1,1);
        var newBoard = new NewScrapboardRequest()
        {
            TripId = 1,
        };
        var message = await HttpClient.PostAsJsonAsync("/api/scrapboard", newBoard);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateScrapboard_BoardAlreadyExists_BadRequest()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = $"test1@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 2,
        });

        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "Test List",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21),
            HasScrapboard = true,
        });
        await context.SaveChangesAsync();
        var newBoard = new NewScrapboardRequest
        {
            TripId = 2,
        };

        var message = await HttpClient.PostAsJsonAsync("/api/scrapboard", newBoard);
        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateScrapboard_NotAuthorised_Unauthorized()
    {
        int userId = 1;
        int tripId = 1;
        await SetupUserAndTrip(userId,tripId);

        await using var context = DbContextFactory.CreateDbContext();
        
        context.Trips.Add(new Trip
        {
            Id = tripId+1,
            Title = "Test List",
            UserId = userId+1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = tripId+1,
        });
        await context.SaveChangesAsync();
        var newBoard = new NewScrapboardRequest
        {
            TripId = 2,
        };

        var message = await HttpClient.PostAsJsonAsync("/api/scrapboard", newBoard);
        message.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetScrapboardThumbnail_ValidData_Ok()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();

        const string fileKey = "thumb-key";

        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = fileKey,
            OriginalFileName = "thumbnail.jpeg",
            MimeType = "image/jpeg",
        });

        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1,
            Thumbnail = 1,
        });
        await context.SaveChangesAsync();

        var filePath = Path.Combine(FakeTestDirectory, fileKey);
        await File.WriteAllBytesAsync(filePath, [1, 2, 3, 4]);

        var message = await HttpClient.GetAsync("/api/scrapboard/1/thumbnail");
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
        message.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
    }

    [Fact]
    public async Task GetScrapboardThumbnail_NotPermittedToAccessBoard_Unauthorized()
    {
        int userId = 1;
        int tripId = 1;
        await SetupUserAndTrip(userId,tripId);

        await using var context = DbContextFactory.CreateDbContext();

        context.Trips.Add(new Trip
        {
            Id = tripId+1,
            Title = "Test List",
            UserId = userId+1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = userId+1,
            FileKey = "thumb-key",
            OriginalFileName = "thumbnail.jpeg",
            MimeType = "image/jpeg",
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = tripId+1,
            Thumbnail = 1,
        });
        await context.SaveChangesAsync();

        var message = await HttpClient.GetAsync("/api/scrapboard/2/thumbnail");
        message.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetScrapboardThumbnail_ScrapboardDoesNotExist_BadRequest()
    {
        await SetupUserAndTrip(1,1);
        var message = await HttpClient.GetAsync("/api/scrapboard/1/thumbnail");
        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetScrapboardThumbnail_FileMissingOnDisk_NotFound()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();

        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "missing-thumb-key",
            OriginalFileName = "thumbnail.jpeg",
            MimeType = "image/jpeg",
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1,
            Thumbnail = 1,
        });
        await context.SaveChangesAsync();

        var message = await HttpClient.GetAsync("/api/scrapboard/1/thumbnail");
        message.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    
    private static readonly byte[] TinyValidJpegBytes =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
        0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
        0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
        0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
        0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
        0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
        0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
        0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03,
        0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00, 0x01, 0x7D,
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06,
        0x13, 0x51, 0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
        0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0, 0x24, 0x33, 0x62, 0x72,
        0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45,
        0x46, 0x47, 0x48, 0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
        0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x73, 0x74, 0x75,
        0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3,
        0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
        0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9,
        0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
        0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4,
        0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01,
        0x00, 0x00, 0x3F, 0x00, 0xFB, 0xD0, 0xFF, 0xD9
    ];
    
    private static MultipartFormDataContent BuildSaveScrapboardForm(
        int tripId,
        string elements = "[]",
        (string fileName, string mimeType, byte[] content)? thumbnail = null,
        (string fileName, string mimeType, byte[] content)[]? files = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(tripId.ToString()), "TripId" },
            { new StringContent(elements), "Elements" },
        };

        var (thumbFileName, thumbMimeType, thumbContent) =
            thumbnail ?? ("thumbnail.jpg", "image/jpeg", TinyValidJpegBytes);
        var thumbnailContent = new ByteArrayContent(thumbContent);
        thumbnailContent.Headers.ContentType = new MediaTypeHeaderValue(thumbMimeType);
        form.Add(thumbnailContent, "Thumbnail", thumbFileName);

        if (files is not null)
        {
            foreach (var (fileName, mimeType, content) in files)
            {
                var fileContent = new ByteArrayContent(content);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                form.Add(fileContent, "Files", fileName);
            }
        }

        return form;
    }

    [Fact]
    public async Task SaveScrapboard_ValidData_Ok()
    {
        await SetupUserAndTrip(1,1);
        await using var context = DbContextFactory.CreateDbContext();
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = 1,
        });
        await context.SaveChangesAsync();

        using var form = BuildSaveScrapboardForm(
            tripId: 1,
            files: [("photo.jpeg", "image/jpeg", TinyValidJpegBytes)]
        );

        var message = await HttpClient.PutAsync("/api/scrapboard/save", form);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveScrapboard_NotPermittedToAccessBoard_Unauthorized()
    {
        int userId = 1;
        int tripId = 1;
        await SetupUserAndTrip(userId,tripId);

        await using var context = DbContextFactory.CreateDbContext();

        context.Trips.Add(new Trip
        {
            Id = tripId+1,
            Title = "Test List",
            UserId = userId+1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2017, 10, 08),
            EndDate = DateToDateTimeOffset(2017, 10, 21)
        });
        context.Scrapboards.Add(new Scrapboard
        {
            TripId = tripId+1,
        });
        await context.SaveChangesAsync();

        using var form = BuildSaveScrapboardForm(tripId: tripId+1);

        var message = await HttpClient.PutAsync("/api/scrapboard/save", form);
        message.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SaveScrapboard_ScrapboardDoesNotExist_BadRequest()
    {
        await SetupUserAndTrip(1,1);
        // No scrapboard created for trip 1, so the service should throw
        // KeyNotFoundException -> BadRequest.
        using var form = BuildSaveScrapboardForm(tripId: 1);

        var message = await HttpClient.PutAsync("/api/scrapboard/save", form);
        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}