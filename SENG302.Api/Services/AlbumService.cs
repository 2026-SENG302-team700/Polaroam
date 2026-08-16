using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Helpers;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services.ImageTagging;
using SENG302.Api.Helpers.Validators;

namespace SENG302.Api.Services;

public interface IAlbumService
{
    public Task<List<PhotoMetadataResponse>> GetImagesMetadataFromAlbumAsync(int tripId);
    public Task<Album> UploadImagesToAlbumAsync(List<IFormFile> photos, int tripId);
    public Task<int?> DeleteImageFromAlbumAsync(int photoId, int tripId);
    public Task<Album> CreateNewAlbumAsync(int tripId);
    public Task<Album> GetAlbumByIdAsync(int tripId);
    public Task<PhotoMetadataResponse> GetSingleImageMetadataFromAlbumAsync(int tripId, int photoId);
}

public class AlbumService : IAlbumService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IFileService _fileService;
    private readonly IImageTaggingQueue _imageTaggingQueue;
    public AlbumService(IFileService fileService,
                        IDbContextFactory<DatabaseContext> dbContextFactory,
                        IImageTaggingQueue imageTaggingQueue)
    {
        _fileService = fileService;
        _imageTaggingQueue = imageTaggingQueue;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Validates that the images given to it are less than 10MB in size, and that they are of a specified type.
    /// For each image that violates these requirements, an error string is added to a list, which is returned 
    /// when all the images in the input list are exhausted.
    /// </summary>
    /// <param name="photos"></param>
    /// <returns>List of error strings</returns>
    private static List<string> ValidateAlbumImages(List<IFormFile> photos)
    {
        var photoErrors = new List<string>();
        foreach (var photo in photos)
        {
            if (!ImageValidator.IsValidFilesize(photo))
            {
                photoErrors.Add($"{photo.FileName}: Image to large, maximum file size is 10MB");
            }
            else if (!ImageValidator.IsValidMimeType(photo))
            {
                photoErrors.Add($"{photo.FileName}: Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp");
            }
        }
        return photoErrors;

    }

    /// <summary>
    /// Given a trip id, return the album associated with that trip. 
    /// Throws KeyNotFoundException if the album does not exist (or the trip doesn't exist)
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<Album> GetAlbumByIdAsync(int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var album = await context.Set<Album>().Where(album => album.TripId == tripId).FirstOrDefaultAsync();
        if (album == null)
        {

            throw new KeyNotFoundException("Album with that Id does not exist.");
        }
        return album;
    }

    /// <summary>
    /// Upon being given a trip-Id and a Photo-Id, this method checks if the album exists 
    /// and then checks to ensure that the image is not a part of a post. If it is, it returns
    /// the post's id.
    /// If the image was not part of the album, it will throw a key not found exception.
    /// Only after this will it attempt deletion, using the fileService delete file method
    /// and removing it from the album.
    /// </summary>
    /// <param name="photoId"></param>
    /// <param name="tripId"></param>
    /// <returns>postId if the photo is part of the post, null otherwise</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task<int?> DeleteImageFromAlbumAsync(int photoId, int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var album = await context.Set<Album>().Where(album => album.TripId == tripId).FirstOrDefaultAsync();
        if (album == null)
        {
            throw new KeyNotFoundException("Album with that Id does not exist.");
        }

        var posts = await context.Set<Post>().Where(post => post.TripId == tripId).ToListAsync();

        foreach (Post post in posts)
        {
            if (post.PhotoIds.Contains(photoId))
            {
                return post.PostId; //returns the postid if the image is part of a post
            }
        }
        if (album.PhotoIds.Remove(photoId))
        {
            CustomFile file;
            file = await _fileService.GetFileByIdAsync(photoId);
            await _fileService.DeleteFileAsync(file.FileKey);
        }
        else
        {
            throw new KeyNotFoundException("Photo with that Id does not exist in this album.");
        }
        return null;
    }


    /// <summary>
    /// Take a list of IFormFiles and tripId. Validate each file and then upload  it
    /// by saving it to the custom files table. Then, get the album of the trip and save
    /// the id of each new photo file to the trip.
    /// </summary>
    /// <param name="photos"></param>
    /// <param name="tripId"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="MultipleValidationListException"></exception>
    public async Task<Album> UploadImagesToAlbumAsync(List<IFormFile> photos, int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var album = await context.Albums.FirstOrDefaultAsync(a => a.TripId == tripId);
        var errors = new Dictionary<string, List<string>>();

        if (album == null)
        {
            throw new KeyNotFoundException("Album with that Id does not exist.");
        }

        List<string> photoErrors = ValidateAlbumImages(photos);

        if (photoErrors.Count > 0)
        {
            errors["photos"] = photoErrors;
            throw new MultipleValidationListException(errors);
        }

        var trip = await context.Set<Trip>().Where(trip => trip.Id == tripId).FirstOrDefaultAsync();
        if (trip == null)
        {
            throw new KeyNotFoundException("Trip does not exist");
        }

        foreach (var photo in photos)
        {
            var file = await _fileService.SaveFileAsync(photo, trip.UserId);
            album.PhotoIds.Add(file.Id);

            try
            {
                FormFile safeFormFile = await ImageTagGeneratorHelper.createFormFileForTagging(photo);

                await _fileService.SetTagQueueStatus(file.Id);

                await _imageTaggingQueue.QueueImageTaggingAsync(
                    new ImageTaggingJob(Image: safeFormFile, ImageId: file.Id)
                );
            }
            catch (Exception ex)
            {
                // Tagging can't cause the image upload to fail
                Console.WriteLine($"Failed to generate tags for image {file.Id}: {ex.Message}");
            }
        }

        await context.SaveChangesAsync();

        return album;
    }


    /// <summary>
    /// Gets all custom files (metadata files) from an album. 
    /// Does this by retrieving an album and then calling the 
    /// file service to retrieve the custom file for all photo-Id.
    /// Returns metadata, and not the actual images.
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<List<PhotoMetadataResponse>> GetImagesMetadataFromAlbumAsync(int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await context.Set<Trip>().Where(trip => trip.Id == tripId).FirstOrDefaultAsync();
        if (trip == null)
        {
            throw new KeyNotFoundException("Trip with that Id does not exist.");
        }

        var album = await context.Set<Album>().Where(album => album.TripId == tripId).FirstOrDefaultAsync();

        if (album == null)
        {
            throw new KeyNotFoundException("Album with that Id does not exist.");
        }

        List<PhotoMetadataResponse> files = [];

        foreach (var photoId in album.PhotoIds)
        {
            try
            {
                var file = await _fileService.GetFileByIdAsync(photoId);
                files.Add(new PhotoMetadataResponse
                {
                    PhotoId = file.Id,
                    FileName = file.OriginalFileName,
                    MimeType = file.MimeType
                });
            } catch (Exception ex)
            {
                Console.WriteLine($"Failed to get file with id {photoId}: {ex.Message}");
            }

        }

        return files;
    }


    /// <summary>
    /// Gets a single customfile object from a trip, given a photoid and tripid.
    /// Returns metadata, and not the actual image.
    /// </summary>
    /// <param name="tripId"></param>
    /// <param name="photoId"></param>
    /// <returns>a customfile object</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<PhotoMetadataResponse> GetSingleImageMetadataFromAlbumAsync(int tripId, int photoId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await context.Set<Trip>().Where(trip => trip.Id == tripId).FirstOrDefaultAsync();
        if (trip == null)
        {
            throw new KeyNotFoundException("Trip with that Id does not exist.");
        }

        var album = await context.Set<Album>().Where(album => album.TripId == tripId).FirstOrDefaultAsync();

        if (album == null)
        {
            throw new KeyNotFoundException("Album with that Id does not exist.");
        }

        var file = await _fileService.GetFileByIdAsync(photoId);

        return new PhotoMetadataResponse
        {
            PhotoId = file.Id,
            FileName = file.OriginalFileName,
            MimeType = file.MimeType
        };
    }


    /// <summary>
    /// Create a new album for a given trip, and returns the album.
    /// Fails if a trip does not exist with that id and throws a KeyNotFoundException.
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns>new album object</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<Album> CreateNewAlbumAsync(int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await context.Set<Trip>().Where(trip => trip.Id == tripId).FirstOrDefaultAsync();
        if (trip == null)
        {
            throw new KeyNotFoundException("Trip with that Id does not exist.");
        }

        var newAlbum = new Album()
        {
            TripId = tripId,
            PhotoIds = []
        };
        context.Set<Album>().Add(newAlbum);

        await context.SaveChangesAsync();

        return newAlbum;
    }

}