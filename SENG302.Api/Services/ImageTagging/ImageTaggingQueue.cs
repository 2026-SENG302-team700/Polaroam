using System.Threading.Channels;
using SENG302.Api.Models.Jobs;

public interface IImageTaggingQueue
{
    ValueTask QueueImageTaggingAsync(ImageTaggingJob job);
    IAsyncEnumerable<ImageTaggingJob> ReadAllAsync(CancellationToken cancellationToken);
}

public class ImageTaggingQueue : IImageTaggingQueue
{
    private readonly Channel<ImageTaggingJob> _queue = Channel.CreateBounded<ImageTaggingJob>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true
    });

    public ValueTask QueueImageTaggingAsync(ImageTaggingJob job)
    {
        return _queue.Writer.WriteAsync(job);
    }
    
    public IAsyncEnumerable<ImageTaggingJob> ReadAllAsync(CancellationToken cancellationToken) => _queue.Reader.ReadAllAsync();
}