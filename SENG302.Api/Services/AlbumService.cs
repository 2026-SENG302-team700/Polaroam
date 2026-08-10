using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using SENG302.Api.Helpers;

public interface IAlbumService
{
    public Task<List<CustomFile>> GetImagesFromAlbumAsync(int tripId);
    public Task<TripAlbum>  UploadImagesToAlbumAsync(List<IFormFile> photos, int tripId);
    public Task DeleteImageFromAlbumAsync(int photoId, int tripId);
}

public class AlbumService: IAlbumService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IFileService _fileService;
    private readonly IImageTaggingService _imageTaggingService;
    public AlbumService(IFileService fileService, 
                        IDbContextFactory<DatabaseContext> dbContextFactory,
                        IImageTaggingService imageTaggingService)
    {
        _fileService = fileService;
        _imageTaggingService = imageTaggingService;
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
    /// Deletes content from bucket first, then from db.
    /// </summary>
    /// <param name="fileKey">fileKey of item to be deleted.</param>
    public async Task DeleteImageFromAlbumAsync(int photoId, int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var album = await context.Set<TripAlbum>().Where(album => album.TripId == tripId).FirstOrDefaultAsync();
        if (album == null)
        {
            throw new KeyNotFoundException("Album with that Id does not exist.");
        }
        if (album.PhotoIds.Remove(photoId))
        {
            var file = await _fileService.GetFileByIdAsync(photoId);
            if (file == null)
            {
                throw new FileNotFoundException("Photo does not exist with that key.");
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
    public async Task<TripAlbum> UploadImagesToAlbumAsync(List<IFormFile> photos, int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var album = await context.Set<TripAlbum>().Where(album => album.TripId == tripId).FirstOrDefaultAsync();
        var errors = new Dictionary<string, List<string>>();

        if (album == null)
        {
            throw new KeyNotFoundException("Album with that Id does not exist.");
        }

        List<string> photoErrors = ValidateAlbumImages(photos);
        
        if (errors.Count > 0)
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
                var tags = await _imageTaggingService.GenerateTagsAsync(photo);
            }
            catch (Exception ex)
            {

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
        var album = await context.Set<TripAlbum>().Where(album => album.TripId == tripId).FirstOrDefaultAsync();

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