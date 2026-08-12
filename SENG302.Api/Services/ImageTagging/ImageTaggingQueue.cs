using System.Threading.Channels;
using SENG302.Api.Models.Jobs;

namespace SENG302.Api.Services.ImageTagging;

public interface IImageTaggingQueue
{
    ValueTask QueueImageTaggingAsync(IFormFile image, int imageId);
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
    /// <param name="image"> The image to be tagged</param>
    /// The images Id
    /// <returns></returns>
    public async ValueTask QueueImageTaggingAsync(IFormFile image, int imageId)
    {   
        try
        {
            var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            ms.Position = 0;

            var safeFormFile = new FormFile(ms, 0, ms.Length, image.Name, image.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = image.ContentType
            };

            await _queue.Writer.WriteAsync(new ImageTaggingJob(Image: safeFormFile, ImageId: imageId));
        }
        catch (Exception ex)
        {
            // Tagging cant cause the image uplaod to fail
            Console.WriteLine($"Failed to generate tags for image {imageId}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Reads from the queue
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<ImageTaggingJob> ReadAllAsync(CancellationToken cancellationToken) => _queue.Reader.ReadAllAsync();
}