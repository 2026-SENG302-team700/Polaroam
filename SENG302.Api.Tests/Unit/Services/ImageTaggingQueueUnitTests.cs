using Microsoft.AspNetCore.Http;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.ImageTagging;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Services;

public class ImageTaggingQueueUnitTests
{
    private static ImageTaggingJob CreateJob(int imageId)
    {
        var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
        var image = new FormFile(stream, 0, stream.Length, "image", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
        return new ImageTaggingJob(image, imageId);
    }

    [Fact]
    public async Task QueueImageTaggingAsync_ValidJob_JobCanBeRead()
    {
        var queue = new ImageTaggingQueue();
        var job = CreateJob(1);

        var queued = queue.QueueImageTaggingAsync(job);
        await using var reader = queue.ReadAllAsync(CancellationToken.None)
            .GetAsyncEnumerator();
        var hasJob = await reader.MoveNextAsync();

        queued.ShouldBeTrue();
        hasJob.ShouldBeTrue();
        reader.Current.ShouldBe(job);
    }

    [Fact]
    public void QueueImageTaggingAsync_QueueIsFull_ReturnsFalse()
    {
        var queue = new ImageTaggingQueue();

        for (var imageId = 1; imageId <= 100; imageId++)
        {
            queue.QueueImageTaggingAsync(CreateJob(imageId)).ShouldBeTrue();
        }

        queue.QueueImageTaggingAsync(CreateJob(101)).ShouldBeFalse();
    }
}
