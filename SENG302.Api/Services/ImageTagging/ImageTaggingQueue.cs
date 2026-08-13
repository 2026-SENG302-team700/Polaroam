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
    /// Creates a new job and adds it to the queue
    /// </summary>
    /// <param name="job"> The image to be tagged</param>
    /// The images ID
    /// <returns></returns>
    public async ValueTask QueueImageTaggingAsync(ImageTaggingJob job)
    {   
        try
        {
            await _queue.Writer.WriteAsync(job);
        }
        catch (Exception ex)
        {
            // Tagging can't cause the image upload to fail
            Console.WriteLine($"Failed to generate tags for image {job.ImageId}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Reads from the queue
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<ImageTaggingJob> ReadAllAsync(CancellationToken cancellationToken) => _queue.Reader.ReadAllAsync();
}