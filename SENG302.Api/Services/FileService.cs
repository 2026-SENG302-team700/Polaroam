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
    Task<bool> CheckFileOwnership(int fileId, int userId);
    Task<List<string>?> GetTagsAsync(int id);
    Task SetTagQueueStatus(int id);
    Task<bool> GetTagQueueStatus(int id);
    Task<List<CustomFile>> GetFilesByIdsAsync(List<int> ids);
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
    /// checks if the given user id matches the ownerid of the file whose id is passed as fileid.
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="userId"></param>
    /// <returns>true if owner is user id, false otherwise</returns>
    public async Task<bool> CheckFileOwnership(int fileId, int userId)
    {
        var file = await GetFileByIdAsync(fileId);
        return file.OwnerId == userId;
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
}