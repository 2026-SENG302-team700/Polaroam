using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Resources.Helpers;


namespace SENG302.Api.Services;

public interface ITaskItemService
{
    Task<IEnumerable<Post>> GetAllPostsAsync(int userId);
    Task<IEnumerable<Post>> GetPostsByTripAsync(int tripId, int userId);
    Task<Post> CreateNewPostAsync(NewPostRequest postRequest, int userId);
    Task<Post?> GetPostAsync(int id, int userId);
    Task<Post> UpdatePostAsync(UpdatePostRequest postUpdates, int userId);
}


public class PostService : ITaskItemService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly TimeProvider _timeProvider;


    public PostService(IDbContextFactory<DatabaseContext> dbContextFactory, TimeProvider timeProvider)
    {
        _dbContextFactory = dbContextFactory;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Get all the tasks from all the lists and check for censoring
    /// </summary>
    /// <returns> an ordered list of all the tasks </returns>
    public async Task<IEnumerable<Post>> GetAllPostsAsync(int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        // get all the task
        var tasks = await context.Set<Post>()
                        .Where(t => context.Set<Trip>()
                        .Any(l => l.Id == t.TripId && l.UserId == userId))
                        .OrderBy(t => t.Date)
                        .ToListAsync();
        // get user
        var user = await context.Users.FindAsync(userId);
        
        if (user?.ProfanityFiltering != true) return tasks;

        var profanityFilter = new ProfanityFilter.ProfanityFilter();
        foreach (Post task in tasks)
        {
            task.Title = profanityFilter.CensorString(task.Title);
            task.Content = profanityFilter.CensorString(task.Content);
        }
        return tasks;
    }

    /// <summary>
    /// Adds a new task to the task list given owned by the given user. All parameters
    /// must be present (except description, dueDate and currentStatus), otherwise it fails. 
    /// The name of the task must be between 3 and 128 characters, and the description
    /// can be up to 2048 characters.
    /// dueDate must be in the future, and currentStatus will be automatically set to
    /// ToDo if not present.
    /// Returns errors if the profanity filter is on and profanity is found in the text
    /// </summary>
    /// <param name="user"></param>
    /// <param name="taskListId"></param>
    /// <param name="name"></param>
    /// <param name="dueDate"></param>
    /// <param name="currentStatus"></param>
    /// <param name="description"></param>
    /// <returns>the newly created TaskItem</returns>
    public async Task<Post> CreateNewPostAsync(NewPostRequest postRequest, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);
        var profanityFiltering = user?.ProfanityFiltering ?? false;

        // Clean request
        postRequest.Title = postRequest.Title.Trim();
        postRequest.Content = postRequest.Content.Trim();

        // Validation
        var errors = PostValidator.ValidatePostFields(postRequest);

        if (errors.Count > 0)
        {
            throw new MultipleValidationException(errors);
        }
        
        // Add task item
        var newTask = new Post()
        {
            TripId = postRequest.TripId,
            Title = postRequest.Title,
            Content = postRequest.Content,
            creationTime = DateTime.UtcNow,
        };
        context.Set<Post>().Add(newTask);
        await context.SaveChangesAsync();
        return newTask;
    }

    /// <summary>
    /// Gets all task items from the given list.
    /// </summary>
    /// <param name="tripId"></param>
    /// <param name="userId"> id of the user making request</param>
    /// <returns>a list of all tasks found. Empty if no tasks exist.</returns>
    public async Task<IEnumerable<Post>> GetPostsByTripAsync(int tripId, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var taskItems = await context.Set<Post>().Where(t => t.TripId == tripId).ToListAsync();

        // check for user profanity status
        var user = await context.Users.FindAsync(userId);

        if (user?.ProfanityFiltering != true) return taskItems;

        var profanityFilter = new ProfanityFilter.ProfanityFilter();
        
        foreach (Post task in taskItems)
        {
            task.Title = profanityFilter.CensorString(task.Title);
            task.Content = profanityFilter.CensorString(task.Content);
        }

        return taskItems;
    }

    public async Task<Post?> GetPostAsync(int id, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);
        var profanityFiltering = user?.ProfanityFiltering ?? false;

        var taskItem = await context.Posts.FirstOrDefaultAsync(t => t.PostId == id);

        if (!profanityFiltering) return taskItem;

        var profanityFilter = new ProfanityFilter.ProfanityFilter();
        var censoredName = profanityFilter.CensorString(taskItem.Title);
        var censoredDescription = profanityFilter.CensorString(taskItem.Content);

        taskItem.Title = censoredName;
        taskItem.Content = censoredDescription;

        return taskItem;
    }

    /// <summary>
    /// Brings in an update task item request from the controller
    /// Strips the name and modifies the due date to be valid
    /// Validates incoming fields
    /// Updates the content then pushes to DB.
    /// </summary>
    /// <param name="postUpdates">Incoming Task Item Request</param>
    /// <returns>The updated task item</returns>
    public async Task<Post> UpdatePostAsync(UpdatePostRequest postUpdates, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(userId);

        var post = await context.Posts.FirstOrDefaultAsync(u => u.PostId == postUpdates.taskId);
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

