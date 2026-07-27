using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Resources.Helpers;
using SENG302.Api.Helpers;


namespace SENG302.Api.Services;

public interface IPostService
{
    Task<IEnumerable<Post>> GetAllPostsAsync(int userId);
    Task<IEnumerable<Post>> GetPostsByTripAsync(int tripId);
    Task<Post> CreateNewPostAsync(NewPostRequest postRequest, List<IFormFile>? photos, int userId);
    Task<Post?> GetPostAsync(int id);
    Task<Post?> UpdatePostAsync(UpdatePostRequest postUpdates);
    Task<bool> UserOwnsPost(int userId, int tripId);
    Task<Post?> AddPhotosToPost(List<int> photoFileIds, int postId);
    Task<List<int>> UploadPhotosToPostAsync(List<IFormFile> files, int postId, int userId);
}


public class PostService : IPostService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IFileService _fileService;


    public PostService(IDbContextFactory<DatabaseContext> dbContextFactory, TimeProvider timeProvider, IFileService fileService)
    {
        _dbContextFactory = dbContextFactory;
        _timeProvider = timeProvider;
        _fileService = fileService;
    }

    /// <summary>
    /// Get all the posts from all the trips
    /// </summary>
    /// <returns> an ordered list of all the posts </returns>
    public async Task<IEnumerable<Post>> GetAllPostsAsync(int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        // get all the posts
        var posts = await context.Set<Post>()
                        .Where(t => context.Set<Trip>()
                        .Any(l => l.Id == t.TripId && l.UserId == userId))
                        .OrderBy(t => t.Date)
                        .ToListAsync();

        return posts;
    }

    /// <summary>
    /// Adds a new post to the trip given owned by the given user. All parameters
    /// must be present (except description), otherwise it fails. 
    /// The name of the post must be between 3 and 128 characters, and the description
    /// can be up to 2048 characters.
    /// dueDate must be in the future, and currentStatus will be automatically set to
    /// </summary>
    /// <param name="postRequest"></param>
    /// <param name="photos"></param>
    /// <param name="userId"></param>
    /// <returns>the newly created postItem</returns>
    public async Task<Post> CreateNewPostAsync(NewPostRequest postRequest, List<IFormFile>? photos, int userId)
    {
        // Clean Request
        postRequest.Title = postRequest.Title.Trim();
        postRequest.Content = postRequest.Content.Trim();
        photos ??= [];
        
        // Validation
        var postErrors = PostValidator.ValidatePostFields(postRequest);
        var errors = postErrors.ToDictionary(kv  => kv.Key, kv => new List<string> { kv.Value });
        
        // Image Validation
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

        if (photoErrors.Count > 0)
        {
            errors["photos"] = photoErrors;
        }

        if (errors.Count > 0)
        {
            throw new MultipleValidationListException(errors);
        }
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Add Photos
        var photoIds = new List<int>();
        foreach (var photo in photos)
        {
            var file = await _fileService.SaveFileAsync(photo, userId);
            photoIds.Add(file.Id);
        }
        
        // Add post
        var newPost = new Post()
        {
            TripId = postRequest.TripId,
            Title = postRequest.Title,
            Content = postRequest.Content,
            creationTime = DateTime.UtcNow,
            Date = postRequest.Date,
            PhotoIds = photoIds,
            Location = postRequest.Location
        };
        context.Set<Post>().Add(newPost);
        await context.SaveChangesAsync();
        return newPost;
    }

    /// <summary>
    /// Gets all Post items from the given trip.
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns>a list of all Posts found. Empty if no Posts exist.</returns>
    public async Task<IEnumerable<Post>> GetPostsByTripAsync(int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var postItems = await context.Set<Post>().Where(t => t.TripId == tripId).ToListAsync();

        return postItems;
    }

    /// <summary>
    /// Given a postId, return the post from the database.
    /// </summary>
    /// <param name="id">the id of the post</param>
    /// <param name="userId"> id of the user making request</param>
    /// <returns>a list of all Posts found. Empty if no Posts exist.</returns>
    public async Task<Post?> GetPostAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var PostItem = await context.Posts.FirstOrDefaultAsync(t => t.PostId == id);
        if (PostItem == null)
        {
            return null;
        }

        return PostItem;
    }

    /// <summary>
    /// Brings in an update Post item request from the controller
    /// Calls UserOwnsPost to verify if the user owns the post or not
    /// Strips the name and modifies the due date to be valid
    /// Validates incoming fields
    /// Updates the content then pushes to DB.
    /// </summary>
    /// <param name="postUpdates">Incoming Post Item Request</param>
    /// <returns>The updated Post item</returns>
    public async Task<Post?> UpdatePostAsync(UpdatePostRequest postUpdates)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        

        var post = await context.Posts.FirstOrDefaultAsync(u => u.PostId == postUpdates.Id);
        if (post == null)
        {
            return null;
        }
        
        postUpdates.Title = postUpdates.Title.Trim();
        postUpdates.Content = postUpdates.Content.Trim();

        var errors = PostValidator.ValidatePostFields(postUpdates, post);

        if (errors.Count > 0)
        {
            throw new MultipleValidationException(errors);
        }

        post.Title = postUpdates.Title;
        post.Content = postUpdates.Content;
        post.Date = postUpdates.Date;

        context.Posts.Update(post);
        await context.SaveChangesAsync();
        return post;
    }
    
    /// <summary>
    /// Adds photos to an existing posts. Called both when post initially created and you want to add extra photos
    /// later on
    /// </summary>
    /// <param name="photoFileIds">List of the custom file Ids corresponding to the photos</param>
    /// <param name="postId">The Ids of the post</param>
    /// <returns></returns>
    public async Task<Post?> AddPhotosToPost(List<int> photoFileIds, int postId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var post = await context.Posts.FirstOrDefaultAsync(u => u.PostId == postId);
        
        if (post == null || postId == null)
        {
            return null;
        }
        
        for (int i = 0; i < photoFileIds.Count; i++)
        {
            post.PhotoIds.Add(photoFileIds[i]);
        }
        
        context.Posts.Update(post);
        await context.SaveChangesAsync();
        return post;
    }

    /// <summary>
    /// Validates and saves photos uploaded for a post, then attaches them to the post.
    /// Deletes the post if any photo fails validation.
    /// currently useless but will be implemented with edit post.
    /// </summary>
    /// <param name="files">The photo files being uploaded</param>
    /// <param name="postId">The id of the post the photos belong to</param>
    /// <param name="userId">The id of the user uploading the photos</param>
    /// <returns>The ids of the newly created custom files for the photos</returns>
    public async Task<List<int>> UploadPhotosToPostAsync(List<IFormFile> files, int postId, int userId)
    {
        var validationErrors = new List<string>();

        foreach (var file in files)
        {
            if (!ImageValidationTools.IsValidFilesize(file))
            {
                validationErrors.Add($"{file.FileName}: Image too large, maximum file size is 10MB");
                continue;
            }

            if (!ImageValidationTools.IsValidMimeType(file))
            {
                validationErrors.Add($"{file.FileName}: Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp");
            }
        }

        if (validationErrors.Count > 0)
        {
            throw new MultipleValidationListException(
                new Dictionary<string, List<string>> {
                    { "photos", validationErrors }
                }
                );
        }

        List<int> customFileIds = [];

        foreach (var file in files)
        {
            var customFile = await _fileService.SaveFileAsync(file, userId);
            customFileIds.Add(customFile.Id);
        }

        await AddPhotosToPost(customFileIds, postId);

        return customFileIds;
    }

    /// <summary>
    /// Given a userId and the post's tripId, check if the user owns the trip
    /// /// </summary>
    /// <param name="userId"></param>
    /// <param name="tripId"></param>
    /// <returns>A boolean indicating if the user owns the trip or not</returns>
    public async Task<bool> UserOwnsPost(int userId, int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var trip = await context.Trips.FirstOrDefaultAsync(u => u.Id == tripId);
        if (trip == null)
        {
            return false;
        }
        if (trip.UserId != userId)
        {
            return false;
        }
        return true;
    }
}

