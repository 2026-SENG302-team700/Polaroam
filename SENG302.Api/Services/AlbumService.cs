using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using SENG302.Api.Helpers;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.ImageTagging;

public interface IAlbumService
{
    public Task<List<CustomFile>> GetImagesFromAlbumAsync(int tripId);
    public Task<Album>  UploadImagesToAlbumAsync(List<IFormFile> photos, int tripId);
    public Task DeleteImageFromAlbumAsync(int photoId, int tripId);
    public Task<Album> GetAlbumByIdAsync(int tripId);
}

public class AlbumService: IAlbumService
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
    private static List<String> ValidateAlbumImages(List<IFormFile> photos)
    {
        var photoErrors = new List<string>();
        foreach (var photo in photos)
        {
            if (!ImageValidationTools.IsValidFilesize(photo))
            {
                photoErrors.Add($"{photo.FileName}: Image to large, maximum file size is 10MB");
            }
            else if (!ImageValidationTools.IsValidMimeType(photo))
            {
                photoErrors.Add($"{photo.FileName}: Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp");
            }
        }
        return photoErrors;

    }

    /// <summary>
    /// given a trip id, return the album associated with that trip. 
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
    /// ***NOT DONE***
    /// </summary>
    /// <param name="photoId"></param>
    /// <param name="tripId"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task DeleteImageFromAlbumAsync(int photoId, int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var album = await context.Set<Album>().Where(album => album.TripId == tripId).FirstOrDefaultAsync();
        if (album == null)
        {
            throw new KeyNotFoundException("Album with that Id does not exist.");
        }
        if (album.PhotoIds.Remove(photoId))
        {
            CustomFile file;
            try
            {
                file = await _fileService.GetFileByIdAsync(photoId);
            }
            catch (KeyNotFoundException e)
            {
                throw new FileNotFoundException(e.Message);
            }
            
            await _fileService.DeleteFileAsync(file.FileKey);
        } else
        {
            throw new KeyNotFoundException("Photo with that Id does not exist in this album.");
        }
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

                await _imageTaggingQueue.QueueImageTaggingAsync(
                    new ImageTaggingJob(Image: safeFormFile, ImageId: file.Id)
                );
            }
            catch (Exception ex)
            {
                // Tagging cant cause the image uplaod to fail
                Console.WriteLine($"Failed to generate tags for image {file.Id}: {ex.Message}");
            }
        }  

        await context.SaveChangesAsync();

        return album;
    }


    /// <summary>
    /// Gets all customfiles from a album. Does this by retrieving an album, and then
    /// calling the file service to retrieve the custom file for all photo Id.
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<List<CustomFile>> GetImagesFromAlbumAsync(int tripId)
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

        List<CustomFile> files = [];

        foreach (var photoId in album.PhotoIds)
        {
            var file = await _fileService.GetFileByIdAsync(photoId);
            files.Add(file);
        }

        return files;
    }
}