using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.ImageTagging;

namespace SENG302.Api.Tests.Unit.Services;

public class ImageTaggingBackgroundServiceUnitTests
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

    private static ImageTaggingBackgroundService CreateWorker(
        IImageTaggingQueue queue,
        IImageTaggingService imageTaggingService
    )
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IImageTaggingService))
            .Returns(imageTaggingService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return new ImageTaggingBackgroundService(queue, scopeFactory);
    }

    [Fact]
    public async Task ExecuteAsync_QueuedJob_GeneratesTags()
    {
        var queue = new ImageTaggingQueue();
        var imageTaggingService = Substitute.For<IImageTaggingService>();
        var processed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var job = CreateJob(1);
        imageTaggingService.GenerateTagsAsync(
                job.Image,
                job.ImageId,
                Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
            {
                processed.TrySetResult();
                return Task.CompletedTask;
            });
        queue.QueueImageTaggingAsync(job);
        var worker = CreateWorker(queue, imageTaggingService);

        await worker.StartAsync(CancellationToken.None);
        await processed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        await imageTaggingService.Received(1).GenerateTagsAsync(
            job.Image,
            job.ImageId,
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_FirstJobFails_ProcessesNextJob()
    {
        var queue = new ImageTaggingQueue();
        var imageTaggingService = Substitute.For<IImageTaggingService>();
        var secondProcessed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var firstJob = CreateJob(1);
        var secondJob = CreateJob(2);
        imageTaggingService.GenerateTagsAsync(
                firstJob.Image,
                firstJob.ImageId,
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromException(new Exception("Tagging failed")));
        imageTaggingService.GenerateTagsAsync(
                secondJob.Image,
                secondJob.ImageId,
                Arg.Any<CancellationToken>()
            )
            .Returns(_ =>
            {
                secondProcessed.TrySetResult();
                return Task.CompletedTask;
            });
        queue.QueueImageTaggingAsync(firstJob);
        queue.QueueImageTaggingAsync(secondJob);
        var worker = CreateWorker(queue, imageTaggingService);

        await worker.StartAsync(CancellationToken.None);
        await secondProcessed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        await imageTaggingService.Received(1).GenerateTagsAsync(
            secondJob.Image,
            secondJob.ImageId,
            Arg.Any<CancellationToken>()
        );
    }
}
