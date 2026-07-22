using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using SENG302.Api.Resources.Helpers;
namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/post")]
public class PostController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPostService _postService;

    public PostController(IPostService postService, IUserService userService)
    {
        _postService = postService;
        _userService = userService;
    }

    /// <summary>
    /// Fetchs all the task items for the current user
    /// </summary>
    /// <returns>All tasks belonging to that user</returns>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Post>>> GetAllPosts()
    {
        // get the logged in user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        try
        {
            var tasks = await _postService.GetAllPostsAsync(int.Parse(userIdString));
            return Ok(tasks);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Fetches all task items associated to the id of the given list. If
    /// no list is provided, then throw a BadRequest Error.
    /// </summary>
    /// <param name="tripId = -1"></param>
    /// <returns>The list of tasks</returns>
    [Authorize]
    [HttpGet("trip/{tripId:int}")]
    public async Task<ActionResult<IEnumerable<Post>>> GetPostsFromTrip(int tripId = -1)
    {
        if (tripId < 0)
        {
            return BadRequest("Trip not provided");
        }
        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("You are not allowed to access this Trip's Posts!");
        }
        var taskList = await _postService.GetPostsByTripAsync(tripId, int.Parse(userIdString));
        return Ok(taskList);
    }

    /// <summary>
    /// Given a post request object, create a new Post and add it to the db
    /// </summary>
    /// <param name="postRequest"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Post>> CreatePost([FromBody] NewPostRequest postRequest)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if(user == null) return Unauthorized();
        
        try
        {
            var response = await _postService.CreateNewPostAsync(postRequest, userId);
            return Ok(response);
        }
        catch (MultipleValidationException e)
        {
            return BadRequest(new BadRequestValidationResponse
            {
                Errors = e.Errors
            });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

    }
    
    /// <summary>
    /// Gets a specific post by its post-Id
    /// </summary>
    /// <param name="id">The desired posts id</param>
    /// <returns>The posts corresponsing to the Id</returns>
    [Authorize]
    [HttpGet("/{id:int}")]
    public async Task<ActionResult<Post>> GetPost(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _postService.GetPostAsync(id, userId);
            return Ok(response);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Given a request to update a task, it will send a request to update that task
    /// to the TaskItem Service
    /// </summary>
    /// <param name="postUpdates">updated values for a task item</param>
    /// <returns>
    /// The updated task item if ok
    /// A Bad Request if an error occured within (likely validation fail).
    /// </returns>
    [Authorize]
    [HttpPut("item/{id:int}")]
    public async Task<ActionResult<Post>> UpdatePost([FromBody] UpdatePostRequest postUpdates)
    {
        var errors = new Dictionary<string, string>();
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        try
        {
            if (ProfanityTools.ContainsProfanity(postUpdates.Content, user.ProfanityFiltering))
            {
                errors["description"] = "Description cannot contain profanity.";
            }
            if (errors.Count > 0)
            {
                return BadRequest(new BadRequestValidationResponse
                {
                    Errors = errors
                });
            }

            var taskItem = await _postService.UpdatePostAsync(postUpdates, userId);
            return Ok(taskItem);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}