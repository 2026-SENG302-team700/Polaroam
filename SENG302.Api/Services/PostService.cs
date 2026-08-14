using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SENG302.Api.Models.Responses;
using SENG302.Api.Helpers;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.ImageTagging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;


namespace SENG302.Api.Services;

public interface IPostService
{
    Task<IEnumerable<Post>> GetAllPostsAsync(int userId);
    Task<IEnumerable<Post>> GetPostsByTripAsync(int tripId);
    Task<Post> CreateNewPostAsync(NewPostRequest postRequest, int userId, List<IFormFile>? photos);
    Task<Post?> GetPostAsync(int id);
    Task<Post?> UpdatePostAsync(UpdatePostRequest postUpdates, int userId, List<IFormFile>? photos);
    Task<bool> UserOwnsPost(int userId, int tripId);
    Task DeletePostByIdAsync(int postId);
    Task DeleteAllPostOfTrip(int tripId);
    Task DeletePhotoFromPostAsync(int postId, int photoId);
}


public class PostService : IPostService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IGeoService _geoService;
    private readonly IFileService _fileService;
    private readonly IImageTaggingQueue _imageTaggingQueue;
    private readonly IAlbumService _albumService;

    public PostService(
        IAlbumService albumService,
        IDbContextFactory<DatabaseContext> dbContextFactory,
        TimeProvider timeProvider,
        IFileService fileService,
        IGeoService geoService,
        IImageTaggingQueue imageTaggingQueue)
    {
        _albumService = albumService;
        _dbContextFactory = dbContextFactory;
        _timeProvider = timeProvider;
        _fileService = fileService;
        _geoService = geoService;
        _imageTaggingQueue = imageTaggingQueue;
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
            .Where(post => context.Set<Trip>()
                .Any(trip => trip.Id == post.TripId && trip.UserId == userId))
            .ToListAsync();

        return posts.OrderBy(post => post.Date);
    }

    /// <summary>
    /// Adds a new post to the trip given owned by the given user. All parameters
    /// must be present (except description), otherwise it fails. 
    /// The name of the post must be between 3 and 128 characters, and the description
    /// can be up to 2048 characters.
    /// dueDate must be in the future, and currentStatus will be automatically set to
    /// </summary>
    /// <param name="postRequest"></param>
    /// <param name="userId"></param>
    /// <param name="photos"></param>
    /// <returns>the newly created postItem</returns>
    public async Task<Post> CreateNewPostAsync(
        NewPostRequest postRequest,
        int userId,
        List<IFormFile>? photos = null
        )
    {
        // Clean Request
        postRequest.Title = postRequest.Title.Trim();
        postRequest.Content = postRequest.Content.Trim();
        photos ??= [];

        // Validation
        var errors = PostValidator.ValidatePostFields(postRequest);

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

        if (errors.Any(e => e.Value.Count > 0))
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
            
            await GenerateImageTags(photo, file.Id);
        }

        Album album = await _albumService.GetAlbumByIdAsync(postRequest.TripId);
       
        album.PhotoIds.AddRange(photoIds);
        context.Albums.Update(album);
        
        await context.SaveChangesAsync();

        // Add location
        var location = await _geoService.GetLocationAsync(postRequest.OsmId);
        if (location == null)
        {
            throw new MultipleValidationListException(new Dictionary<string, List<string>>
            {
                ["location"] = ["This isn't a valid location"]
            });
        }

        // Add post
        var newPost = new Post
        {
            TripId = postRequest.TripId,
            Title = postRequest.Title,
            Content = postRequest.Content,
            creationTime = _timeProvider.GetUtcNow(),
            Date = postRequest.Date,
            PhotoIds = photoIds,
            OsmId = postRequest.OsmId,
            LocationName = location.name,
        };
        context.Set<Post>().Add(newPost);
        await context.SaveChangesAsync();

        context.Set<Location>().Add(new Location
        {
            OsmId = postRequest.OsmId,
            PostId = newPost.PostId,
            Lat = location.lat,
            Lon = location.lon,
            Name = location.name,
            DisplayName = location.display_name,
        });

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
    /// <returns>a list of all Posts found. Empty if no Posts exist.</returns>
    public async Task<Post?> GetPostAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var postItem = await context.Posts.FirstOrDefaultAsync(t => t.PostId == id);
        return postItem;
    }

    /// <summary>
    /// Brings in an update Post item request from the controller
    /// Calls UserOwnsPost to verify if the user owns the post or not
    /// Strips the name and modifies the due date to be valid
    /// Validates incoming fields
    /// Updates the content then pushes to DB.
    /// </summary>
    /// <param name="postUpdates">Incoming Post Item Request</param>
    /// <param name="userId"></param>
    /// <param name="photos"></param>
    /// <returns>The updated Post item</returns>
    public async Task<Post?> UpdatePostAsync(
        UpdatePostRequest postUpdates,
        int userId,
        List<IFormFile>? photos = null
        )
    {
        // Clean Request
        postUpdates.Title = postUpdates.Title.Trim();
        postUpdates.Content = postUpdates.Content.Trim();
        photos ??= [];

        // Validation
        var errors = PostValidator.ValidatePostFields(postUpdates);

        // Image Validation
        var photoErrors = new List<string>();
        foreach (var photo in photos)
        {
            if (!ImageValidationTools.IsValidFilesize(photo))
            {
                photoErrors.Add($"{photo.FileName}: Image too large, maximum file size is 10MB");
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

        if (errors.Any(e => e.Value.Count > 0))
        {
            throw new MultipleValidationListException(errors);
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Select post
        var post = await context.Posts.FirstOrDefaultAsync(u => u.PostId == postUpdates.Id);
        if (post == null)
        {
            throw new KeyNotFoundException();
        }

        // Location Validation
        GeoLocationSearchResult? location = null;

        if (postUpdates.OsmId != post.OsmId)
        {
            location = await _geoService.GetLocationAsync(postUpdates.OsmId);
            if (location == null)
            {
                throw new MultipleValidationListException(new Dictionary<string, List<string>>
                {
                    ["location"] = ["This isn't a valid location"]
                });
            }
        }

        // Add / Remove Photos 
        List<int> customFileIds = [];

        foreach (var photo in photos)
        {
            var customFile = await _fileService.SaveFileAsync(photo, userId);
            customFileIds.Add(customFile.Id);

            await GenerateImageTags(photo, customFile.Id);
        }
        
        // get the album and add new photo Ids
        Album album = await _albumService.GetAlbumByIdAsync(post.TripId);
       
        album.PhotoIds.AddRange(customFileIds);
        context.Albums.Update(album);
        
        await context.SaveChangesAsync();
        

        // Update post content
        post.Title = postUpdates.Title;
        post.Content = postUpdates.Content;
        post.Date = postUpdates.Date;
        post.OsmId = postUpdates.OsmId;
        post.PhotoIds = post.PhotoIds.Count > 0 ? post.PhotoIds = post.PhotoIds.Concat(customFileIds).ToList() : post.PhotoIds = customFileIds;
        post.LocationName = location != null ? location.name : post.LocationName;

        // Save Changes
        context.Posts.Update(post);
        await context.SaveChangesAsync();
        return post;
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
        return trip.UserId == userId;
    }

    /// <summary>
    /// Delete a specific post by its id from the database and remove accociated images along with it.
    /// </summary>
    /// <param name="postId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task DeletePostByIdAsync(int postId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var post = await context.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
        
        if (post == null)
        {
            throw new ArgumentException(
                "Post with the provided ID does not exist."
            );
        }
        var album = await context.Albums.FirstOrDefaultAsync(a => a.TripId == post.TripId);

        var locations = await context.Locations
            .Where(location => location.PostId == postId)
            .ToListAsync();

        context.Locations.RemoveRange(locations);

        if (post.PhotoIds.Count > 0)
        {
            var images = await context.CustomFiles
                .Where(file => post.PhotoIds.Contains(file.Id))
                .ToListAsync();

            foreach (var image in images)
            {
                await _fileService.DeleteFileAsync(image.FileKey);
                album.PhotoIds.Remove(image.Id);
            }
        }

        context.Posts.Remove(post);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes all posts of a given trip. Used when a trip is deleted.
    /// </summary>
    /// <param name="tripId"></param>
    public async Task DeleteAllPostOfTrip(int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var posts = context.Set<Post>().Where(p => p.TripId == tripId).ToList();

        for (int i = 0; i < posts.Count; i++)
        {
            await DeletePostByIdAsync(posts[i].PostId);
        }

    }


    /// <summary>
    /// Deletes post photo via file service then removes from photo id list.
    /// </summary>
    /// <param name="postId">The post id containing the image</param>
    /// <param name="photoId">photo id of deleted photo</param>
    /// <exception cref="KeyNotFoundException">photo not belonging in post</exception>
    public async Task DeletePhotoFromPostAsync(int postId, int photoId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var post = await context.Posts.FirstOrDefaultAsync(t => t.PostId == postId);
        if (post == null)
        {
            throw new KeyNotFoundException(
                "Post of this Id does not exist."
            ); 
        }
        if (post.PhotoIds == null || !post.PhotoIds.Contains(photoId))
        {
            throw new KeyNotFoundException(
                "This photo does not belong to this post."
            );
        }
        var album = await context.Albums.FirstOrDefaultAsync(a => a.TripId == post.TripId);

        var photo = await _fileService.GetFileByIdAsync(photoId);
        await _fileService.DeleteFileAsync(photo.FileKey);

        post.PhotoIds.Remove(photoId);
        if (album != null)
        {
            album.PhotoIds.Remove(photoId);    
        }
        
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Private method used to make a clean call to the image tagging service and return the error if it fails,
    /// while still allowing the image upload to continue
    /// </summary>
    /// <param name="image"></param>
    /// <param name="imageId"></param>
    /// <returns></returns>
    private async Task GenerateImageTags(IFormFile image, int imageId)
    {
        try
        {
            FormFile safeFormFile = await ImageTagGeneratorHelper.createFormFileForTagging(image);

            await _fileService.SetTagQueueStatus(imageId);
            
            await _imageTaggingQueue.QueueImageTaggingAsync(
                new ImageTaggingJob(Image: safeFormFile, ImageId: imageId)
            );
        }
        catch (Exception ex)
        {
            // Tagging cant cause the image uplaod to fail
            Console.WriteLine($"Failed to generate tags for image {imageId}: {ex.Message}");
        }
    }
}

