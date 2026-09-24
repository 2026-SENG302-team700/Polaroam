using Microsoft.AspNetCore.Http;
using SENG302.Api.Services;
using Shouldly;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace SENG302.Api.Tests.Integration.Services;

public class FileServiceTests : BaseIntegrationTestFixture
{
    private IFileService ServiceUnderTest => ServiceProvider.GetRequiredService<IFileService>();

    public FileServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory) { }

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
    public async Task SaveFileEntity_Success_ReturnsCustomFileEntity()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");


        await ServiceUnderTest.SaveFileAsync(mockFile, 0);

        var singleItemInDb = context.CustomFiles.ShouldHaveSingleItem();
        singleItemInDb.OwnerId.ShouldBe(0);
        singleItemInDb.OriginalFileName.ShouldBe("mocka");
        singleItemInDb.MimeType.ShouldBe("image/webp");
        singleItemInDb.FileSize.ShouldBe(mockFile.Length);

        var readFile = File.ReadAllBytes(Path.Combine(FakeTestDirectory, singleItemInDb.FileKey));
        readFile.ShouldBe(mockFileContent);
    }

    [Fact]
    public async Task GetFileEntity_Success_ReturnsCustomFileEntity()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        var fileEntity = await ServiceUnderTest.SaveFileAsync(mockFile, 0);

        var getFileEntity = await ServiceUnderTest.GetFileByIdAsync(fileEntity.Id);
        getFileEntity.ShouldBeEquivalentTo(fileEntity);
    }

    [Fact]
    public async Task GenerateFileKey_FileInput_FileNameDifferFileKey()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        var fileKey = ServiceUnderTest.GenerateFileKey(mockFile);
        fileKey.ShouldNotBe(mockFile.FileName);
    }

    [Fact]
    public async Task GetFileContent_Valid_ReturnsFileContentyBytes()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        var fileEntity = await ServiceUnderTest.SaveFileAsync(mockFile, 0);

        var newFile = await ServiceUnderTest.GetFileContentAsync(fileEntity.FileKey);
        newFile.ShouldBe(mockFileContent);
    }

    [Fact]
    public async Task GetFileId_InvalidID_KeyNotFoundException()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.GetFileByIdAsync(22);
        });
    }

    [Fact]
    public async Task DeleteFile_Valid_DeleteFileFromDiskDb()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        var fileEntity = await ServiceUnderTest.SaveFileAsync(mockFile, 0);

        await ServiceUnderTest.DeleteFileAsync(fileEntity.FileKey);

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.GetFileByIdAsync(fileEntity.Id);
        });
    }

    [Fact]
    public async Task RemoveTagAsync_HasTags_ReturnsOk()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        List<string> insertTags = new List<string>(["ich bin gut", "germanic", "ich bin sprechen ze deutsch", "remove"]);

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
            Tags = insertTags
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();


        await Should.NotThrowAsync(async () => await ServiceUnderTest.RemoveTagAsync(file.Id, "remove"));
        var tags = await ServiceUnderTest.GetTagsAsync(file.Id);
        tags.ShouldNotBeNull();
        tags.ShouldBeEquivalentTo(file.Tags.Where(u => u != "remove").ToList());
    }

    [Fact]
    public async Task RemoveTagAsync_RemoveAll_NoneLeft()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        List<string> insertTags = new List<string>(["tag1", "tag2"]);

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
            Tags = insertTags
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        await Should.NotThrowAsync(async () => await ServiceUnderTest.RemoveTagAsync(file.Id, "tag1"));
        await Should.NotThrowAsync(async () => await ServiceUnderTest.RemoveTagAsync(file.Id, "tag2"));

        var tags = await ServiceUnderTest.GetTagsAsync(file.Id);
        tags.ShouldNotBeNull();
        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveTagAsync_TagNonexistent_Error()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        List<string> insertTags = new List<string>(["tag1", "tag2"]);

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
            Tags = insertTags
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        await Should.ThrowAsync<TagNotFoundException>(async () => await ServiceUnderTest.RemoveTagAsync(file.Id, "tag3"));

        var tags = await ServiceUnderTest.GetTagsAsync(file.Id);
        tags.ShouldNotBeNull();
        tags.ShouldBeEquivalentTo(file.Tags);
    }

    [Fact]
    public async Task RemoveTagAsync_FileNonexistent_Error()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        List<string> insertTags = new List<string>(["tag1", "tag2"]);

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
            Tags = insertTags
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        await Should.ThrowAsync<KeyNotFoundException>(async () => await ServiceUnderTest.RemoveTagAsync(100, "tag1"));

        var tags = await ServiceUnderTest.GetTagsAsync(file.Id);
        tags.ShouldNotBeNull();
        tags.ShouldBeEquivalentTo(file.Tags);
    }

    [Fact]
    public async Task GetTagsAsync_HasTags_ReturnsTags()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        List<string> insertTags = new List<string>(["ich bin gut", "germanic", "ich bin sprechen ze deutsch"]);

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
            Tags = insertTags
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        var tags = await ServiceUnderTest.GetTagsAsync(file.Id);
        tags.ShouldNotBeNull();
        tags.ShouldBeEquivalentTo(file.Tags);
    }

    [Fact]
    public async Task GetTagsAsync_HasNoTags_ReturnsFalse()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        var tags = await ServiceUnderTest.GetTagsAsync(file.Id);
        tags.ShouldBeNull();
    }

    [Fact]
    public async Task GetTagsAsync_ImageDoesntExist_ThrowsKeyNotFoundException()
    {
        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.GetTagsAsync(1);
        });
    }

    [Fact]
    public async Task SetTagQueueStatus_validId_SetInTagQueueTrue()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        await ServiceUnderTest.SetTagQueueStatus(1);
        await using var newContext = await DbContextFactory.CreateDbContextAsync();
        var newFile = await newContext.CustomFiles.FirstOrDefaultAsync(f => f.Id == file.Id);
        newFile.ShouldNotBeNull();
        newFile.InTagQueue.ShouldBeTrue();
    }

    [Fact]
    public async Task SetTagQueueStatus_invalidId_throwsKeyNotFound()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        await Should.ThrowAsync<KeyNotFoundException>(async () =>
        {
            await ServiceUnderTest.SetTagQueueStatus(2);
        });

    }

    [Fact]
    public async Task GetTagQueueStatus_ValidIdNotInQueue_ReturnsFalse()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        var status = await ServiceUnderTest.GetTagQueueStatus(1);
        status.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTagQueueStatus_ValidIdInQueue_ReturnsTrue()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
        };

        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        await ServiceUnderTest.SetTagQueueStatus(1);

        var status = await ServiceUnderTest.GetTagQueueStatus(1);
        status.ShouldBeTrue();
    }

    [Fact]
    public async Task GetMimeTypeAsync_ValidId_ReturnsString()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        CustomFile file = new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "mocka",
            OriginalFileName = "du bist gut genug.png",
            MimeType = "image/png",
            FileSize = 100,
        };
        context.CustomFiles.Add(file);
        await context.SaveChangesAsync();

        var type = await ServiceUnderTest.GetMimeTypeAsync(file.Id);
        type.ShouldNotBeNull();
        type.ShouldBe("image/png");
    }

    /// <summary>
    /// Make a mock trip, album and user
    /// </summary>
    /// <param name="tripId">The id of the trip</param>
    /// <param name="ownerId">The id of the owner of the trip</param>
    /// <param name="collaborators">The collaborators on the trip</param>
    /// <param name="albumPhotoIds">The ids of the photos in the trip album</param>
    private async Task MakeTrip(int tripId, int ownerId, List<int> collaborators, List<int> albumPhotoIds)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        context.Trips.Add(
            new Trip()
            {
                Id = tripId,
                Collaborators = collaborators,
                Title = "title",
                StartDate = FakeTimeProvider.GetUtcNow().AddDays(-1),
                EndDate = FakeTimeProvider.GetUtcNow(),
                UserId = ownerId
            }
        );

        context.Albums.Add(
            new Album()
            {
                TripId = tripId,
                PhotoIds = albumPhotoIds
            }
        );

        context.Users.Add(
            new User()
            {
                Id = ownerId,
                Email = $"example{ownerId}@example.com",
                DisplayName = "John Example",
                Country = "US"
            }
        );

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Make a mock trip, album, user, and post
    /// </summary>
    /// <param name="tripId">The id of the trip</param>
    /// <param name="ownerId">The id of the owner of the trip</param>
    /// <param name="collaborators">The collaborators on the trip</param>
    /// <param name="postId">The id of the post</param>
    /// <param name="postOwnerId">The id of the poster of the post</param>
    /// <param name="postPhotoIds">The ids of the photos in the post</param>
    private async Task MakeTripAndPost(int tripId, int ownerId, List<int> collaborators, int postId, int postOwnerId, List<int> postPhotoIds)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        context.Trips.Add(
            new Trip()
            {
                Id = tripId,
                Collaborators = collaborators,
                Title = "title",
                StartDate = FakeTimeProvider.GetUtcNow().AddDays(-1),
                EndDate = FakeTimeProvider.GetUtcNow(),
                UserId = ownerId
            }
        );

        context.Albums.Add(
            new Album()
            {
                TripId = tripId,
                PhotoIds = []
            }
        );

        context.Users.Add(
            new User()
            {
                Id = ownerId,
                Email = $"example{ownerId}@example.com",
                DisplayName = "John Example",
                Country = "US"
            }
        );

        context.Posts.Add(
            new Post()
            {
                PostId = postId,
                TripId = tripId,
                Title = "Title",
                Date = FakeTimeProvider.GetUtcNow(),
                OsmId = 1,
                OsmType = "node",
                LocationName = "Mars",
                PhotoIds = postPhotoIds,
                CreatorId = postOwnerId,
                CreationTime = FakeTimeProvider.GetUtcNow()
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task FileAccessCheck_IsTripOwnerAndPhotoInAlbum_ReturnTrue()
    {
        int tripId = 100;
        int userId = 200;
        int photoId = 300;
        await MakeTrip(tripId, userId, [], [photoId]);

        Assert.True(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_NotOwnerOrCollaboratorAndPhotoInAlbum_ReturnFalse()
    {
        int tripId = 100;
        int userId = 200;
        int ownerId = 300;
        int photoId = 400;
        await MakeTrip(tripId, ownerId, [], [photoId]);

        Assert.False(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_IsCollaboratorAndPhotoInAlbum_ReturnTrue()
    {
        int tripId = 100;
        int userId = 200;
        int ownerId = 300;
        int photoId = 400;
        await MakeTrip(tripId, ownerId, [userId], [photoId]);

        Assert.True(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_IsPostOwnerAndPhotoInPost_ReturnTrue()
    {
        int tripId = 100;
        int postId = 200;
        int postOwnerId = 300;
        int tripOwnerId = 400;
        int userId = postOwnerId;
        int photoId = 600;
        await MakeTripAndPost(tripId, tripOwnerId, [userId], postId, postOwnerId, [photoId]);

        Assert.True(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_IsTripOwnerAndPhotoInPost_ReturnTrue()
    {
        int tripId = 100;
        int postId = 200;
        int postOwnerId = 300;
        int tripOwnerId = 400;
        int userId = tripOwnerId;
        int photoId = 600;
        await MakeTripAndPost(tripId, tripOwnerId, [], postId, postOwnerId, [photoId]);

        Assert.True(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_IsTripCollaboratorAndPhotoInPost_ReturnTrue()
    {
        int tripId = 100;
        int postId = 200;
        int postOwnerId = 300;
        int tripOwnerId = 400;
        int userId = 500;
        int photoId = 600;
        await MakeTripAndPost(tripId, tripOwnerId, [userId], postId, postOwnerId, [photoId]);

        Assert.True(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_PhotoInAlbumOfOtherTrip_ReturnFalse()
    {
        int photoId = 100;

        int otherTripId = 10;
        int otherTripOwnerId = 20;
        await MakeTrip(otherTripId, otherTripOwnerId, [], [photoId]);

        int tripId = 200;
        int tripOwnerId = 300;
        int userId = tripOwnerId;
        await MakeTrip(tripId, userId, [], []);

        Assert.False(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_PhotoInPostOfOtherTrip_ReturnFalse()
    {
        int photoId = 100;

        int otherTripId = 10;
        int otherTripOwnerId = 20;
        int otherPostId = 30;
        int otherPostOwnerId = 40;
        await MakeTripAndPost(otherTripId, otherTripOwnerId, [], otherPostId, otherPostOwnerId, [photoId]);

        int tripId = 200;
        int tripOwnerId = 300;
        int userId = tripOwnerId;
        await MakeTrip(tripId, userId, [], []);

        Assert.False(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_PhotoIsInPostAndWePostedThatPostButForSomeReasonWeWereKickedFromThatTripSomeTimeAgo_ReturnFalse()
    {
        int userId = 100;
        int photoId = 200;

        int tripId = 10;
        int tripOwnerId = 20;
        int postId = 30;
        int postOwnerId = userId;
        await MakeTripAndPost(tripId, tripOwnerId, [], postId, postOwnerId, [photoId]);

        Assert.False(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }

    [Fact]
    public async Task FileAccessCheck_PhotoIdNowhere_ReturnFalse()
    {
        int tripId = 100;
        int userId = 200;
        int ownerId = 300;
        int photoId = 400;
        await MakeTrip(tripId, ownerId, [], []);

        Assert.False(await ServiceUnderTest.UserHasAccessToFileFromTrip(userId, tripId, photoId));
    }
}