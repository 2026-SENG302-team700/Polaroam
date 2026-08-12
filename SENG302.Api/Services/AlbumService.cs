
using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Helpers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using SENG302.Api.Services.ImageTagging;

public interface IAlbumService
{
    public Task<List<CustomFile>> GetImagesFromAlbumAsync(int tripId);
    public Task UploadImagesToAlbumAsync(List<IFormFile>? images, int tripId, int userId);
    public Task DeleteImageFromAlbumAsync(int photoId);
}

public class AlbumService : IAlbumService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IFileService _fileService;
    private readonly IImageTaggingQueue _imageTaggingQueue;

    public AlbumService(
        IDbContextFactory<DatabaseContext> dbContextFactory,
        IFileService fileService,
        IImageTaggingQueue imageTaggingQueue)
    {
        _dbContextFactory = dbContextFactory;
        _fileService = fileService;
        _imageTaggingQueue = imageTaggingQueue;
    }

    public async Task<List<CustomFile>> GetImagesFromAlbumAsync(int tripId)
    {
        var images = new List<CustomFile>();
        return images;
    }
    
    
    /// <summary>
    /// Validates, uploads, queues for tagging, and sets album images in trip
    /// </summary>
    /// <param name="images">The images to be uploaded</param>
    /// <param name="tripId">The trip the photos are being uploaded to</param>
    /// <param name="userId">The uploaders user Id</param>
    /// <exception cref="ValidationListException">If the images contain any validation errors</exception>
    /// <exception cref="ArgumentException">If the trip is not found</exception>
    public async Task UploadImagesToAlbumAsync(List<IFormFile>? images, int tripId, int userId)
    {
        // Validation
        if (images == null) throw new ArgumentException("No images provided");
        var imageErrors = new List<string>();
        foreach (var image in images)
        {
            if (!ImageValidationTools.IsValidFilesize(image))
            {
                imageErrors.Add($"{image.FileName}: Image too large, maximum file size is 10MB");
            }
            else if (!ImageValidationTools.IsValidMimeType(image))
            {
                imageErrors.Add($"{image.FileName}: Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp");
            }
        }
        if (imageErrors.Count > 0) throw new ValidationListException(imageErrors);
        
        // Add Photos
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var imageIds = new List<int>();
        foreach (var image in images)
        {
            var file = await _fileService.SaveFileAsync(image, userId);
            imageIds.Add(file.Id);
            await _imageTaggingQueue.QueueImageTaggingAsync(image, file.Id);
        }
        
        // Update trip to include new images
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
        if (trip == null) throw new ArgumentException("Trip not found");
        trip.AlbumPhotoIds.AddRange(imageIds);
        await context.SaveChangesAsync();
    }

    public async Task DeleteImageFromAlbumAsync(int photoId)
    {
        
    }

}