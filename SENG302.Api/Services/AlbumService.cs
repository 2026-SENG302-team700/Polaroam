
using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Helpers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services.ImageTagging;

namespace SENG302.Api.Services;
public interface IAlbumService
{
    public Task<List<CustomFile>> GetImagesFromAlbumAsync(int tripId);
    public Task UploadImagesToAlbumAsync(List<IFormFile>? images, int tripId, int userId);
    public Task DeleteImageFromAlbumAsync(int tripId, int imageId);
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
    
    /// <summary>
    /// Returns all images that have been added as part of the photo album and all images
    /// that have been added to a post in the trip
    /// </summary>
    /// <param name="tripId">The trip we are returning all images for</param>
    /// <returns>A list of images</returns>
    public async Task<List<CustomFile>> GetImagesFromAlbumAsync(int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
        var postPhotoIds = await context.Posts
            .Where(p => p.TripId == tripId)
            .Select(p => p.PhotoIds)
            .ToListAsync(); // List<List<int>>, materialized

        var photoIds = (trip?.AlbumPhotoIds ?? [])
            .Concat(postPhotoIds.SelectMany(ids => ids))
            .Distinct()
            .ToList();

        return await context.CustomFiles
            .Where(f => photoIds.Contains(f.Id))
            .ToListAsync();
    }
    
    
    /// <summary>
    /// Validates, uploads, queues for tagging, and sets album images in the trip
    /// </summary>
    /// <param name="images">The images to be uploaded</param>
    /// <param name="tripId">The trip the photos are being uploaded to</param>
    /// <param name="userId">The uploaders user ID</param>
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
    
    /// <summary>
    /// Deletes an image from the album and removes it from the trip if it is part of the trip album.
    /// </summary>
    /// <param name="tripId">The trip the image is a part of</param>
    /// <param name="imageId">The image to be deleted</param>
    /// <exception cref="ArgumentException">For all validation errors</exception>
    /// <exception cref="InvalidOperationException">If image is included in a post</exception>
    public async Task DeleteImageFromAlbumAsync(int tripId, int imageId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
        if (trip == null) throw new ArgumentException("Trip not found");
        
        var image = await context.CustomFiles.FirstOrDefaultAsync(f => f.Id == imageId);
        if (image == null) throw new ArgumentException("Image not found");
        
        // Remove from trip album
        if (trip.AlbumPhotoIds.Contains(imageId))
        {
            trip.AlbumPhotoIds.Remove(imageId);
            context.CustomFiles.Remove(image);
            await context.SaveChangesAsync();
            return;
        }
        
        // Check if it's in a post and if so, throw an exception with the post-ID
        var post = await context.Posts.FirstOrDefaultAsync(p => p.PhotoIds.Contains(imageId));
        if (post != null)
        {
            throw new InvalidOperationException(post.PostId.ToString());
        }
        
        // If not part of a post or trip album, throw an exception
        throw new ArgumentException("Image does not belong to the trip");
    }

}