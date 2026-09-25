using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services.ImageTagging;

namespace SENG302.Api.Services;

public interface IFileService
{
    Task<CustomFile> SaveFileAsync(IFormFile file, int ownerId);
    Task<CustomFile> GetFileByIdAsync(int id);
    Task DeleteFileAsync(string fileKey);
    Task<byte[]> GetFileContentAsync(string fileKey);
    string GenerateFileKey(IFormFile file);
    Task<bool> UserHasAccessToFileFromTrip(int userId, int tripId, int fileId);
    Task<List<string>?> GetTagsAsync(int id);
    Task RemoveTagAsync(int id, string tag);
    Task SetTagQueueStatus(int id);
    Task<bool> GetTagQueueStatus(int id);
    public Task<string?> GetTagErrors(int id);
    Task<List<CustomFile>> GetFilesByIdsAsync(List<int> ids);
    public Task<string> GetMimeTypeAsync(int fileId);
}

public class FileService : IFileService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly string _basePath;


    public FileService(
        IDbContextFactory<DatabaseContext> dbContextFactory,
        IConfiguration config
        )
    {
        _dbContextFactory = dbContextFactory;
        _basePath = config["FileStorage:BasePath"];
        if (string.IsNullOrEmpty(_basePath))
        {
            throw new InvalidOperationException("Base path not set");
        }
        Directory.CreateDirectory(_basePath);
    }

    /// <summary>
    /// Generates a safe file key to use in directory bucket
    /// </summary>
    /// <param name="file">The file we are saving</param>
    /// <returns>A generated safe file key to use in our bucket storage.</returns>
    public string GenerateFileKey(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        var fileKey = $"{Guid.NewGuid()}{extension}";
        return fileKey;
    }

    /// <summary>
    /// Creates new file and copies byte information over to it
    /// </summary>
    /// <param name="file">The file to copy to the new file</param>
    /// <param name="fileKey">File key of new file</param>
    public async Task WriteFile(IFormFile file, string fileKey)
    {
        try
        {
            var path = Path.Combine(_basePath, fileKey);
            await using var stream = File.Create(path);
            await file.CopyToAsync(stream);
        }
        catch (Exception e)
        {
            throw new IOException(e.Message);
        }

    }

    /// <summary>
    /// Saves a file entity into the db.
    /// </summary>
    /// <param name="file">File to save into db</param>
    /// <param name="ownerId">Owner of the file</param>
    /// <param name="fileKey">File Key of the file to save into db</param>
    /// <returns>Returns saved entity as an object.</returns>
    public async Task<CustomFile> SaveFileEntity(IFormFile file, int ownerId, string fileKey)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var customFile = new CustomFile
        {
            OwnerId = ownerId,
            FileKey = fileKey,
            OriginalFileName = file.FileName,
            MimeType = file.ContentType,
            FileSize = file.Length
        };

        context.CustomFiles.Add(customFile);
        await context.SaveChangesAsync();

        return customFile;
    }

    /// <summary>
    /// Takes an incoming file and follows the following steps
    /// >> Validate Information (needs to be implemented -- see u7-image-validation)
    /// >> Generates safe filename key
    /// >> Writes file to disk
    /// >> Saves file entity to db
    /// </summary>
    /// <param name="file">File containing information to be saved</param>
    /// <param name="ownerId">Foreign Key to who "owns" the file</param>
    /// <returns>The custom file entity object</returns>
    public async Task<CustomFile> SaveFileAsync(IFormFile file, int ownerId)
    {
        var fileKey = GenerateFileKey(file);
        await WriteFile(file, fileKey);
        var customFile = await SaveFileEntity(file, ownerId, fileKey);
        return customFile;
    }

    /// <summary>
    /// Fetch a custom file entity from the database that matches the passed id
    /// DOES NOT RETURN THE FILE ITSELF, just an entity that has reference to the file
    /// </summary>
    /// <param name="id">ID of customfile entity</param>
    /// <returns>Return a custom file entity.</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<CustomFile> GetFileByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.CustomFiles.FindAsync(id) ?? throw new KeyNotFoundException(
            $"File {id} not found"
            );
    }

    /// <summary>
    /// Fetches the content from a file from the given file key
    /// </summary>
    /// <param name="fileKey">FileKey used to lookup file in bucket</param>
    /// <returns>File content of matching file key</returns>
    public async Task<byte[]> GetFileContentAsync(string fileKey)
    {
        try
        {
            var path = Path.Combine(_basePath, fileKey);
            var content = await File.ReadAllBytesAsync(path);
            return content;
        }
        catch (Exception e)
        {
            throw new FileNotFoundException("File does not exist but has key", e.Message);
        }
    }

    /// <summary>
    /// Deletes content from bucket first, then from db.
    /// </summary>
    /// <param name="fileKey">fileKey of item to be deleted.</param>
    public async Task DeleteFileAsync(string fileKey)
    {
        var path = Path.Combine(_basePath, fileKey);
        File.Delete(path);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.CustomFiles.Where(f => f.FileKey == fileKey).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Returns a files tags.
    /// </summary>
    /// <param name="id">the file id.</param>
    /// <returns>
    /// Null: File has no tags.
    /// Array of strings that are the tags.
    /// </returns>
    public async Task<List<string>?> GetTagsAsync(int id)
    {
        var file = await GetFileByIdAsync(id);
        return file.Tags;
    }

    /// <summary>
    /// Removes a specific tag from a file
    /// </summary>
    /// <param name="id">the id of the file we are removing a tag from</param>
    /// <param name="tag">the tag we are removing, as a string</param>
    /// <returns></returns>
    public async Task RemoveTagAsync(int id, string tag)
    {
        var file = await GetFileByIdAsync(id);

        if (file.Tags == null || !file.Tags.Contains(tag)) throw new TagNotFoundException($"Tag '{tag}' is not found");

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        file.Tags.Remove(tag);
        context.Update(file);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Sets the status of a file that it is in the queue to be tagged or the job is being performed.
    /// </summary>
    /// <param name="id">ID of the image to be set as being tagged</param>
    /// <exception cref="KeyNotFoundException">File not found.</exception>
    public async Task SetTagQueueStatus(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var file = await context.CustomFiles.FirstOrDefaultAsync(f => f.Id == id);

        if (file == null)
        {
            throw new KeyNotFoundException("File not found.");
        }

        file.InTagQueue = true;
        context.CustomFiles.Update(file);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Checks to see whether a file is within the tagging queue or job is being performed.
    /// </summary>
    /// <param name="id">ID of the image to be checked if being tagged</param>
    /// <returns>
    /// True: File is in the queue or being processed.
    /// False: File is not in the queue and not being processed.
    /// </returns>
    public async Task<bool> GetTagQueueStatus(int id)
    {
        var file = await GetFileByIdAsync(id);
        return file.InTagQueue;
    }

    public async Task<string?> GetTagErrors(int id)
    {
        var file = await GetFileByIdAsync(id);
        return file.TagError;
    }

    /// <summary>
    /// gets custom file entities for provided ids.
    /// </summary>
    /// <param name="ids">file id's</param>
    /// <returns>The file entites.</returns>
    public async Task<List<CustomFile>> GetFilesByIdsAsync(List<int> ids)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.CustomFiles.Where(f => ids.Contains(f.Id)).ToListAsync();
    }

    /// <summary>
    /// gets the mime type of a custom file
    /// </summary>
    /// <param name="fileId">the custom file</param>
    /// <returns>the mime type of the custom file</returns>
    public async Task<string> GetMimeTypeAsync(int fileId)
    {
        var file = await GetFileByIdAsync(fileId);
        return file.MimeType;
    }


    /// <summary>
    /// Checks if a user has access to a file through a trip
    /// It checks if the file is contained within a post, or within the trip's algum
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="tripId">The id of the trip</param>
    /// <param name="fileId">The id of the file</param>
    /// <returns>Whether the user has access to the file from the trip</returns>
    public async Task<bool> UserHasAccessToFileFromTrip(int userId, int tripId, int fileId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // If the user cannot access the trip then say no
        var userCanAccessTrip = await context.Trips.Where(t => t.Id == tripId && (t.Collaborators.Contains(userId) || t.UserId == userId)).AnyAsync();
        if (!userCanAccessTrip) return false;

        // If the file is in the trip album then say yes
        var fileIsInAlbum = await context.Albums.Where(a => a.TripId == tripId && a.PhotoIds.Contains(fileId)).AnyAsync();
        if (fileIsInAlbum) return true;

        // If the file is in a post of the trip then say yes
        var fileIsInPost = await context.Posts.Where(p => p.TripId == tripId && p.PhotoIds.Contains(fileId)).AnyAsync();
        if (fileIsInPost) return true;

        return false;
    }
}