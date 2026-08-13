using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using SENG302.Api.Services.ImageTagging;

namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/image")]
public class ImageController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IFileService _fileService;
    private readonly IImageTaggingQueue _imageTaggingQueue;
    private readonly IImageService _imageService;


    public ImageController(
        IUserService userService, 
        IFileService fileService, 
        IImageTaggingQueue imageTaggingQueue,
        IImageService imageService
    )
    {
        _userService = userService;
        _fileService = fileService;
        _imageTaggingQueue = imageTaggingQueue;
        _imageService = imageService;
    }

    /// <summary>
    /// Returns the tags of the requests photo
    /// </summary>
    /// <param name="photoId">id of photo</param>
    /// <returns>an array of strings containing the image tags</returns>
    [Authorize]
    [HttpGet("{photoId:int}/tags")]
    public async Task<IActionResult> GetPhotoTags(int photoId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if (user == null || !await _fileService.CheckFileOwnership(photoId, userId))
        {
            return Unauthorized();
        }

        try
        {
            var tags = await _fileService.GetTagsAsync(photoId);
            if (tags == null)
            {
                var imageInQueue = await _fileService.GetTagQueueStatus(photoId);

                return imageInQueue
                    ? Ok("Tags are being generated.")
                    : StatusCode(StatusCodes.Status204NoContent, "This photo has no tags.");
            }
            return Ok(tags);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    /// <summary>
    /// Returns a list of suggested tags for the user
    /// based on the query provided. Tags are selected from real
    /// tags belonging to the user.
    /// Max 5 tags are returned.
    /// </summary>
    /// <param name="query">The string to match with</param>
    /// <returns>
    /// 200 - List containing the suggested tags 
    /// Unauthorized - User is not authenticated
    /// BadRequest - Unknown error
    /// </returns>
    [HttpGet("tags/{query:alpha}")]
    public async Task<ActionResult> GetSuggestedTags(string query)
    {
        try
        {
            // Get the user id so only their tags are returned
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null )
            {
                return Unauthorized();
            }
            
            // Get the suggested tags
            var tags = await _imageService.GetSuggestedTagsForUser(userId, query);
            return Ok(tags);
        }
        catch (Exception) // Unknown error
        {
            return BadRequest("Failed to get suggested tags.");
        }
    }
    
    /// <summary>
    /// Returns a list of suggested tags for the user
    /// based on the query provided. Tags are selected from real
    /// tags belonging to the user from the given trip.
    /// Max 5 tags are returned.
    /// </summary>
    /// <param name="query">The string to match with</param>
    /// <param name="tripId">The id of the trip to get tags from</param>
    /// <returns>
    /// 200 - List containing the suggested tags 
    /// Unauthorized - User is not authenticated
    /// BadRequest - Unknown error
    /// </returns>
    [HttpGet("tags/trip/{tripId:int}")]
    public async Task<ActionResult> GetSuggestedTagsForTrip(string query, int tripId)
    {
        try
        {
            // Get the user id so only their tags are returned
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null )
            {
                return Unauthorized();
            }
            
            // Get the suggested tags
            var tags = await _imageService.GetSuggestedTagsForUser(userId, query, tripId);
            return Ok(tags);
        }
        catch (Exception) // Unknown error
        {
            return BadRequest("Failed to get suggested tags.");
        }
    }
}