using SENG302.Api.Services.ImageTagging;
using Microsoft.AspNetCore.Http;
using SENG302.Api.Services;
using Shouldly;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.Models.Entities;
using NSubstitute;
using SENG302.Api.Models.Jobs;

namespace SENG302.Api.Tests.Integration.Services;

public class ImageTaggingServiceTests : BaseIntegrationTestFixture
{
    private IImageTaggingService ServiceUnderTest => ServiceProvider.GetRequiredService<IImageTaggingService>();

    private IFileService _fileService => ServiceProvider.GetRequiredService<IFileService>();

    public ImageTaggingServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory) { }

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
    public async Task QueueImageTaggingJob_Success_TaggingCannotBeRetried()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        CustomFile file = await _fileService.SaveFileAsync(mockFile, 0);

        ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(true);

        await ServiceUnderTest.QueueImageTaggingJobs(mockFile, file.Id);

        CustomFile updatedFile = await _fileService.GetFileByIdAsync(file.Id);

        updatedFile.CanRetryTagging.ShouldBeFalse();
        updatedFile.TagError.ShouldBeNull();

    }

    [Fact]
    public async Task QueueImageTaggingJob_Failure_TaggingCanBeRetriedAndErrorNotNull()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        CustomFile file = await _fileService.SaveFileAsync(mockFile, 0);

        ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(false);

        await ServiceUnderTest.QueueImageTaggingJobs(mockFile, file.Id);

        CustomFile updatedFile = await _fileService.GetFileByIdAsync(file.Id);

        updatedFile.CanRetryTagging.ShouldBeTrue();
        updatedFile.TagError.ShouldNotBeNull();
        updatedFile.TagError.ShouldBe("Tagging failed: Queue was full");
    }

    [Fact]
    public async Task QueueImageTaggingJob_InvalidImageId_Something()
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();

        var mockFileContent = File.ReadAllBytes("resources/panda.webp");

        var mockFile = GetMockFile(
            "mocka",
            mockFileContent,
            "image/webp");

        CustomFile file = await _fileService.SaveFileAsync(mockFile, 0);

        Exception exception = await Should.ThrowAsync<Exception>(async () =>
        {
            await ServiceUnderTest.QueueImageTaggingJobs(mockFile, 67);
        });

        exception.Message.ShouldContain($"Failed to generate tags for image 67");

    }
}