using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Resources.Helpers;


namespace SENG302.Api.Services;

public interface IPostService
{
    Task<IEnumerable<Post>> GetAllPostsAsync(int userId);
    Task<IEnumerable<Post>> GetPostsByTripAsync(int tripId, int userId);
    Task<Post> CreateNewPostAsync(NewPostRequest postRequest, int userId);
    Task<Post?> GetPostAsync(int id, int userId);
    Task<Post?> UpdatePostAsync(UpdatePostRequest postUpdates, int userId);
}


public class PostService : IPostService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly TimeProvider _timeProvider;


    public PostService(IDbContextFactory<DatabaseContext> dbContextFactory, TimeProvider timeProvider)
    {
        _dbContextFactory = dbContextFactory;
        _timeProvider = timeProvider;
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
    /// Returns errors if the profanity filter is on and profanity is found in the text
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="postRequest"></param>
    /// <returns>the newly created postItem</returns>
    public async Task<Post> CreateNewPostAsync(NewPostRequest postRequest, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);

        // Clean request
        postRequest.Title = postRequest.Title.Trim();
        postRequest.Content = postRequest.Content.Trim();

        // Validation
        var errors = PostValidator.ValidatePostFields(postRequest);

        if (errors.Count > 0)
        {
            throw new MultipleValidationException(errors);
        }

        // Add post
        var newPost = new Post()
        {
            TripId = postRequest.TripId,
            Title = postRequest.Title,
            Content = postRequest.Content,
            creationTime = DateTime.UtcNow,
            Date = postRequest.Date,
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
    /// <param name="userId"> id of the user making request</param>
    /// <returns>a list of all Posts found. Empty if no Posts exist.</returns>
    public async Task<IEnumerable<Post>> GetPostsByTripAsync(int tripId, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var postItems = await context.Set<Post>().Where(t => t.TripId == tripId).ToListAsync();


        // check for user profanity status
        var user = await context.Users.FindAsync(userId);

        return postItems;
    }

    public async Task<Post?> GetPostAsync(int id, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);
        var PostItem = await context.Posts.FirstOrDefaultAsync(t => t.PostId == id);
        if (PostItem == null)
        {
            return null;
        }

        PostItem.Title = PostItem.Title;
        PostItem.Content = PostItem.Content;

        return PostItem;
    }

    /// <summary>
    /// Brings in an update Post item request from the controller
    /// Strips the name and modifies the due date to be valid
    /// Validates incoming fields
    /// Updates the content then pushes to DB.
    /// </summary>
    /// <param name="postUpdates">Incoming Post Item Request</param>
    /// <returns>The updated Post item</returns>
    public async Task<Post?> UpdatePostAsync(UpdatePostRequest postUpdates, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);

        var post = await context.Posts.FirstOrDefaultAsync(u => u.PostId == postUpdates.PostId);
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
}

