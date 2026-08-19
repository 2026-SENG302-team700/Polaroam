using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using Shouldly;
using Xunit.Abstractions;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class ImageControllerTest : BaseIntegrationTestFixture
{
    private ITestOutputHelper _output;

    public ImageControllerTest(WebApplicationFactory<Program> webApplicationFactory, ITestOutputHelper output) : base(
        webApplicationFactory)
    {
        _output = output;
    }

    /// <summary>
    /// Get a defulat mock file for the testing
    /// </summary>
    /// <param name="filename">file name of the file to create</param>
    /// <param name="content"> content of the file to create</param>
    /// <param name="mimeType">mimetype of file to create</param>
    /// <returns></returns>
    private IFormFile GetMockFile(string filename, Byte[] content, string mimeType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(),
            ContentType = mimeType
        };
    }

    /// <summary>
    /// Create a new user within database along with a trip and album owned by that user
    /// </summary>
    /// <param name="userId">If of the user to create</param>
    /// <param name="email">email of the user to create</param>
    /// <returns></returns>
    private async Task SetupUserAndTrip(int userId, string email)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User { Id = userId, Email = email, DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = userId, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 8, 9, 14, 30, 0, TimeSpan.FromHours(12)) });
        context.Albums.Add(new Album { TripId = 1, PhotoIds = new() });
        await context.SaveChangesAsync();
    }
    
    [Fact]
    public async Task GetPhotoTags_ValidId_ReturnsOk()
    {
        List<string> tags = ["Berlin", "German", "Bratwurst", "Volkswagen", "Lederhosen"];
        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.FromHours(12)), });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
            Tags = tags
        });
        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newTags = await response.Content.ReadFromJsonAsync<List<string>>();
        newTags.ShouldNotBeNull();
        foreach (var tag in newTags)
        {
            tags.ShouldContain(tag);
        }
    }
    
    private async Task SetupSecondTrip(int userId, int tripId)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Trips.Add(new Trip { Id = tripId, Title = "Second trip", UserId = userId, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 9, 8, 14, 30, 0, TimeSpan.FromHours(12)) });
        context.Albums.Add(new Album { TripId = tripId, PhotoIds = new() });
        await context.SaveChangesAsync();
    }

    private async Task AddTaggedImagesToAlbum(int tripId, params (int Id, List<string>? Tags)[] files)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        var album = context.Albums.First(a => a.TripId == tripId);
        foreach (var (id, tags) in files)
        {
            album.PhotoIds.Add(id);
            context.CustomFiles.Add(new CustomFile
            {
                Id = id,
                FileKey = "",
                MimeType = "",
                OriginalFileName = "",
                OwnerId = 1,
                Tags = tags
            });
        }
        await context.SaveChangesAsync();
    }

    private async Task SetupPost(int tripId, int postId)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Posts.Add(new Post
        {
            PostId = postId, TripId = tripId, Title = "My post", Content = "", PhotoIds = [], OsmId = 1,
            LocationName = "my home"
        });
        await context.SaveChangesAsync();
    }

    private async Task SetupImageContext(bool belongsToPost, int fileOwnerId = 1)
    {
        await SetupUserAndTrip(1, "test@example.com");

        await using var context = await DbContextFactory.CreateDbContextAsync();
        const int photoId = 10;

        context.CustomFiles.Add(new CustomFile
        {
            Id = photoId,
            OwnerId = fileOwnerId,
            FileKey = "image-context-test.webp",
            OriginalFileName = "image-context-test.webp",
            MimeType = "image/webp"
        });

        var album = await context.Albums.SingleAsync(a => a.TripId == 1);
        album.PhotoIds.Add(photoId);

        if (belongsToPost)
        {
            context.Posts.Add(new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "My post",
                Content = "Post content",
                PhotoIds = [photoId],
                OsmId = 1,
                LocationName = "Christchurch"
            });
        }

        await context.SaveChangesAsync();
    }

    // GetImageContext Tests
    [Fact]
    public async Task GetImageContext_ImageBelongsToPost_ReturnsTripAndPostContext()
    {
        await SetupImageContext(belongsToPost: true);

        var response = await HttpClient.GetAsync("/api/image/10/context");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var imageContext = await response.Content.ReadFromJsonAsync<ImageContextResponse>();
        imageContext.ShouldNotBeNull();
        imageContext.TripId.ShouldBe(1);
        imageContext.TripTitle.ShouldBe("My trip");
        imageContext.PostId.ShouldBe(1);
        imageContext.PostTitle.ShouldBe("My post");
    }

    [Fact]
    public async Task GetImageContext_ImageBelongsOnlyToAlbum_ReturnsTripContextWithoutPost()
    {
        await SetupImageContext(belongsToPost: false);

        var response = await HttpClient.GetAsync("/api/image/10/context");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var imageContext = await response.Content.ReadFromJsonAsync<ImageContextResponse>();
        imageContext.ShouldNotBeNull();
        imageContext.TripId.ShouldBe(1);
        imageContext.TripTitle.ShouldBe("My trip");
        imageContext.PostId.ShouldBeNull();
        imageContext.PostTitle.ShouldBeNull();
    }

    [Fact]
    public async Task GetImageContext_ImageOwnedByAnotherUser_ReturnsUnauthorized()
    {
        await SetupImageContext(belongsToPost: false, fileOwnerId: 2);

        var response = await HttpClient.GetAsync("/api/image/10/context");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetImageContext_ImageDoesNotExist_ReturnsNotFound()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var response = await HttpClient.GetAsync("/api/image/999/context");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<int> SetupPostPhotos(int postId, byte[] fileBytes, string mimeType)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(postId.ToString()), "Id" },
            { new StringContent("1"), "TripId" },
            { new StringContent("Valid title"), "Title" },
            { new StringContent("Valid content"), "Content" },
            { new StringContent("1"), "osmId" },
            { new StringContent(DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")), "Date" }
        };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        form.Add(fileContent, "files", "test");

        var response = await HttpClient.PutAsync($"/api/post/{postId}", form);
        response.EnsureSuccessStatusCode();

        var updatedPost = await response.Content.ReadFromJsonAsync<Post>();
        return updatedPost!.PhotoIds.Single();
    }

    // GetSuggestedTags Tests
    [Fact]
    public async Task GetSuggestedTags_ValidQuery_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["beach", "sunset", "mountain"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        _output.WriteLine(await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_PartialMatch_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["beachside", "sunset"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beachside"]);
    }

    [Fact]
    public async Task GetSuggestedTags_CaseInsensitive_ReturnsMatchingTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["Beach", "Sunset"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["Beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_NoMatchingTags_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["mountain", "forest"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTags_NoAlbums_ReturnsEmptyList()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestedTags_FilesWithNullTags_AreIgnored()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, null), (2, ["beach"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_DuplicateTagsAcrossFiles_ReturnsDistinctTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1,
            (1, ["beach", "sunset"]),
            (2, ["beach", "mountain"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }

    [Fact]
    public async Task GetSuggestedTags_AcrossMultipleTrips_ReturnsAllOwnedTags()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await SetupSecondTrip(1, 2);
        await AddTaggedImagesToAlbum(1, (1, ["beachfront"]));
        await AddTaggedImagesToAlbum(2, (2, ["beachball"]));

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beachball", "beachfront"], ignoreOrder: true);
    }
    
    [Fact]
    public async Task GetSuggestedTags_OtherUsersTags_AreNotReturned()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await AddTaggedImagesToAlbum(1, (1, ["beach"]));

        // A trip/album/tag belonging to a different user
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Users.Add(new User { Id = 404, Email = "notMe@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });
            context.Trips.Add(new Trip { Id = 99, Title = "Other trip", UserId = 404, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 9, 8, 14, 30, 0, TimeSpan.FromHours(12)) });
            context.Albums.Add(new Album { TripId = 99, PhotoIds = [2] });
            context.CustomFiles.Add(new CustomFile { Id = 2, FileKey = "", MimeType = "", OriginalFileName = "", OwnerId = 404, Tags = ["beachhouse"] });
            await context.SaveChangesAsync();
        }

        var response = await HttpClient.GetAsync("/api/image/tags?query=beach");
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<string[]>();
        tags.ShouldBe(["beach"]);
    }
    
    [Fact]
    public async Task GetPhotoTags_ValidIdInQueue_ReturnsOk()
    {

        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.FromHours(12)),});
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
            InTagQueue = true
        });
        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("Tags are being generated.");
    }
    
    [Fact]
    public async Task GetPhotoTags_ValidIdNoTags_ReturnsNoContent()
    {

        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)), EndDate = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.FromHours(12)), });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
        });

        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

    }

    [Fact]
    public async Task RetryPhotoTagging_ValidIdAIEnabled_ReturnsOK()
    {
        await SetupUserAndTrip(1, "test@example.com");
        
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var user = await context.Users.Where(u => u.Id == 1).FirstOrDefaultAsync();
        user.AiTaggingEnabled = true;
        user.AcceptedAiPrivacyPolicy = true;
        context.Users.Update(user);
        await context.SaveChangesAsync();
        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");
        
        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");
        
        await context.SaveChangesAsync();
        
        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        CustomFile customFile = await fileService.SaveFileAsync(mockFile, ownerId: 1);
        customFile.TagError = "Queue was full";
        context.CustomFiles.Update(customFile);
        await context.SaveChangesAsync();
        
        ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(false);
        
        var response = await HttpClient.PatchAsync("/api/image/1/tags", null);
        
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task RetryPhotoTagging_AIPolicyNotAccepted_ReturnsBadRequest()
    {
        await SetupUserAndTrip(1, "test@example.com");
        
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var user = await context.Users.Where(u => u.Id == 1).FirstOrDefaultAsync();
        user.AiTaggingEnabled = true;
        context.Users.Update(user);
        await context.SaveChangesAsync();
        
        var mockFileContent = File.ReadAllBytes("resources/panda.webp");
        
        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        await context.SaveChangesAsync();

        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        CustomFile customFile = await fileService.SaveFileAsync(mockFile, ownerId: 1);
        customFile.TagError = "Queue was full";
        context.CustomFiles.Update(customFile);
        await context.SaveChangesAsync();

        ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(false);

        var response = await HttpClient.PatchAsync("/api/image/1/tags", null);

        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        content.ShouldBe("Image cannot be tagged");
    }

    [Fact]
    public async Task RetryPhotoTagging_AITaggingNotEnabled_ReturnsBadRequest()
    {
        await SetupUserAndTrip(1, "test@example.com");

        await using var context = await DbContextFactory.CreateDbContextAsync();

        var user = await context.Users.Where(u => u.Id == 1).FirstOrDefaultAsync();
        user.AcceptedAiPrivacyPolicy = true;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        await context.SaveChangesAsync();

        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        CustomFile customFile = await fileService.SaveFileAsync(mockFile, ownerId: 1);
        customFile.TagError = "Queue was full";
        context.CustomFiles.Update(customFile);
        await context.SaveChangesAsync();

        ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(false);

        var response = await HttpClient.PatchAsync("/api/image/1/tags", null);

        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        content.ShouldBe("Image cannot be tagged");
    }

    [Fact]
    public async Task RetryPhotoTagging_TaggingErrorNotDueToQueue_ReturnsBadRequest()
    {
        await SetupUserAndTrip(1, "test@example.com");

        await using var context = await DbContextFactory.CreateDbContextAsync();

        var user = await context.Users.Where(u => u.Id == 1).FirstOrDefaultAsync();
        user.AiTaggingEnabled = true;
        user.AcceptedAiPrivacyPolicy = true;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        await context.SaveChangesAsync();

        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        CustomFile customFile = await fileService.SaveFileAsync(mockFile, ownerId: 1);
        customFile.TagError = "AI failed to tag image";
        context.CustomFiles.Update(customFile);
        await context.SaveChangesAsync();

        ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(false);

        var response = await HttpClient.PatchAsync("/api/image/1/tags", null);

        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        content.ShouldBe("Image cannot be tagged");
    }

    [Fact]
    public async Task RetryPhotoTagging_TagErrorNull_ReturnsBadRequest()
    {
        await SetupUserAndTrip(1, "test@example.com");

        await using var context = await DbContextFactory.CreateDbContextAsync();

        var user = await context.Users.Where(u => u.Id == 1).FirstOrDefaultAsync();
        user.AiTaggingEnabled = true;
        user.AcceptedAiPrivacyPolicy = true;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        await context.SaveChangesAsync();

        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        CustomFile customFile = await fileService.SaveFileAsync(mockFile, ownerId: 1);

        ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(false);

        var response = await HttpClient.PatchAsync("/api/image/1/tags", null);

        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        content.ShouldBe("Image cannot be tagged");
    }

    [Fact]
    public async Task RetryPhotoTagging_CanRetryTaggingIsFalse_ReturnsBadRequest()
    {
        await SetupUserAndTrip(1, "test@example.com");

        await using var context = await DbContextFactory.CreateDbContextAsync();

        var user = await context.Users.Where(u => u.Id == 1).FirstOrDefaultAsync();
        user.AiTaggingEnabled = true;
        user.AcceptedAiPrivacyPolicy = true;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        await context.SaveChangesAsync();

        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        CustomFile customFile = await fileService.SaveFileAsync(mockFile, ownerId: 1);
        customFile.CanRetryTagging = false;
        context.CustomFiles.Update(customFile);
        await context.SaveChangesAsync();

        ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(false);

        var response = await HttpClient.PatchAsync("/api/image/1/tags", null);

        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        content.ShouldBe("Image cannot be tagged");
    }


    [Fact]
    public async Task RetryPhotoTagging_UserDoesNotOwnFile_ReturnsUnauthorized()
    {
        await SetupUserAndTrip(1, "test@example.com");

        await using var context = await DbContextFactory.CreateDbContextAsync();

        var user = await context.Users.Where(u => u.Id == 1).FirstOrDefaultAsync();
        user.AiTaggingEnabled = true;
        user.AcceptedAiPrivacyPolicy = true;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        await context.SaveChangesAsync();

        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        CustomFile customFile = await fileService.SaveFileAsync(mockFile, ownerId: 2);
        customFile.CanRetryTagging = false;
        context.CustomFiles.Update(customFile);
        await context.SaveChangesAsync();

        var response = await HttpClient.PatchAsync("/api/image/1/tags", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }


    [Fact]
    public async Task GetPostPhoto_ValidPhotoId_ReturnsPhoto()
    {
        await SetupUserAndTrip(1, "test@example.com");
        await SetupPost(1, 1);

        var fileBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        var photoId = await SetupPostPhotos(1, fileBytes, "image/webp");

        var response = await HttpClient.GetAsync($"/api/image/{photoId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        responseBytes.SequenceEqual(fileBytes).ShouldBeTrue();
    }

    [Fact]
    public async Task GetPostPhoto_PhotoIdDoesNotExist_ReturnsNotFound()
    {
        await SetupUserAndTrip(1, "test@example.com");

        var response = await HttpClient.GetAsync("/api/image/999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }


    /// <summary>
    /// Used for the remove tag checkings
    /// </summary>
    /// <param name="tags">The list of tags the created Image will have</param>
    /// <returns></returns>
    private async Task AddImageWithTags(List<string> tags, int userId = 1)
    {
        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Users.Add(new User
        {
            Id = userId,
            Email = $"tag-owner-{userId}@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "My trip",
            UserId = userId,
            StartDate = new DateTimeOffset(2026, 8, 8, 14, 30, 0, TimeSpan.FromHours(12)),
            EndDate = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.FromHours(12))
        });
        context.Posts.Add(new Post
        {
            TripId = 1,
            Title = "My post",
            Content = "",
            OsmId = 292806332,
            LocationName = "Christchurch"
        });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = userId,
            FileKey = "abc",
            OriginalFileName = "test.jpg",
            MimeType = "image/jpeg",
            Tags = tags
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveTagAsync_HasTags_ReturnsOk()
    {
        await AddImageWithTags(["a", "b", "c"]);

        var response = await HttpClient.DeleteAsync("/api/image/1/tags/a");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveTagAsync_TagNonexistent_NotFound()
    {
        await AddImageWithTags(["a", "b", "c"]);

        var response = await HttpClient.DeleteAsync("/api/image/1/tags/d");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveTagAsync_FileNonexistent_NotFound()
    {
        await AddImageWithTags(["a", "b", "c"]);

        var response = await HttpClient.DeleteAsync("/api/image/2/tags/a");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveTagAsync_NotRegisteredUser_Unauthorized()
    {
        await AddImageWithTags(["a", "b", "c"], 100);

        var response = await HttpClient.DeleteAsync("/api/image/1/tags/a");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveTagAsync_NotOwner_Unauthorized()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "current@example.com",
                DisplayName = "Current User",
                PasswordKey = "password",
                Country = "Test Country"
            });
            await context.SaveChangesAsync();
        }

        await AddImageWithTags(["a", "b", "c"], 100);

        var response = await HttpClient.DeleteAsync("/api/image/1/tags/a");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
