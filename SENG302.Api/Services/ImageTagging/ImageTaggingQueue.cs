using System.Threading.Channels;
using SENG302.Api.Models.Jobs;

namespace SENG302.Api.Services.ImageTagging;

public interface IImageTaggingQueue
{
    ValueTask QueueImageTaggingAsync(ImageTaggingJob job);
    IAsyncEnumerable<ImageTaggingJob> ReadAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// A queue for image tagging jobs
/// </summary>
public class ImageTaggingQueue : IImageTaggingQueue
{
    private readonly Channel<ImageTaggingJob> _queue = Channel.CreateBounded<ImageTaggingJob>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true
    });
    
    /// <summary>
    /// Adds a job to the queue
    /// </summary>
    /// <param name="job"></param>
    /// <returns></returns>
    public ValueTask QueueImageTaggingAsync(ImageTaggingJob job)
    {
        return _queue.Writer.WriteAsync(job);
    }
    
    /// <summary>
    /// Reads from the queue
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<ImageTaggingJob> ReadAllAsync(CancellationToken cancellationToken) => _queue.Reader.ReadAllAsync();
}