namespace SENG302.Api.Services.ImageTagging;

public class ImageTaggingBackgroundService : BackgroundService
{
    private readonly IImageTaggingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;

    public ImageTaggingBackgroundService(IImageTaggingQueue queue, IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var job in _queue.ReadAllAsync(cancellationToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var imageTaggingService = scope.ServiceProvider.GetRequiredService<IImageTaggingService>();

                await imageTaggingService.GenerateTagsAsync(job.Image, job.ImageId);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to tag image: {exception.Message}");
            }
        }
    }
}