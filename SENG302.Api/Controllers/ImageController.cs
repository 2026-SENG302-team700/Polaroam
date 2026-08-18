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
    private readonly IImageTaggingService _imageTaggingService;
    private readonly IImageService _imageService;


    public ImageController(
        IUserService userService, 
        IFileService fileService, 
        IImageService imageService,
        IImageTaggingService imageTaggingService
    )
    {
        _userService = userService;
        _fileService = fileService;
        _imageService = imageService;
        _imageTaggingService = imageTaggingService;
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

            if (tags != null)
            {
                return Ok(tags);
            }
            
            var error = await _fileService.GetTagErrors(photoId);
            
            if (error != null)
            {
                return Problem(detail: error,
                    statusCode: 500);
            }
            
            
            var imageInQueue = await _fileService.GetTagQueueStatus(photoId);

            return imageInQueue
                ? Ok("Tags are being generated.")
                : NoContent();
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
[HttpGet("tags")]
public async Task<ActionResult> GetSuggestedTags([FromQuery] string query)
{
    try
    {
        // Get the user id so only their tags are returned
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        // Get the suggested tags
        var tags = await _imageService.GetSuggestedTagsForUser(userId, query);
        return Ok(tags);
    }
    catch (Exception e) // Unknown error
    {
        return BadRequest("Failed to get suggested tags." + e.Message);
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
    public async Task<ActionResult> GetSuggestedTagsForTrip([FromQuery] string query, int tripId)
    {
        try
        {
            // Get the user id so only their tags are returned
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
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

    /// <summary>
    /// This endpoint is called when you want to retry tagging, if tagging failed previously due to the queue being
    /// too full.
    /// </summary>
    /// <param name="photoId">The id of the file/image</param>
    /// <returns>
    /// 200 - If nothing goes wrong
    /// Bad Request - If an exception is thrown
    /// Unauthorized - If the user is not logged in or does not own the file/image
    /// </returns>
    [Authorize]
    [HttpPatch("{photoId:int}/tags")]
    public async Task<IActionResult> RetryPhotoTagging(int photoId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if (user == null || !await _fileService.CheckFileOwnership(photoId, userId))
        {
            return Unauthorized();
        }
        
        // Check if photoId already has error about queue being too full. If it does and user has tagging enabled, try re-tag
        CustomFile file = await _fileService.GetFileByIdAsync(photoId);
        if (file.CanRetryTagging && user.AcceptedAiPrivacyPolicy && user.AiTaggingEnabled && file.TagError != null && file.TagError.Contains("Queue"))
        {
            byte[] bytes = await _fileService.GetFileContentAsync(file.FileKey);
            var stream = new MemoryStream(bytes);
            IFormFile formFile = new FormFile(
                baseStream: stream,
                baseStreamOffset: 0,
                length: stream.Length,
                name: file.OriginalFileName,
                fileName: file.OriginalFileName
            ){
                Headers = new HeaderDictionary(),
                ContentType = file.MimeType
            };

            try
            {
                await _imageTaggingService.QueueImageTaggingJobs(formFile, photoId);
                
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        return BadRequest("Image cannot be tagged");
    }
    
    /// <summary>
    /// Streams a single photo back with correct types
    /// </summary>
    /// <param name="photoId">id of photo</param>
    /// <returns>an image file</returns>
    [Authorize]
    [HttpGet("{photoId:int}")]
    public async Task<IActionResult> GetPostPhoto(int photoId)
    {
        try
        {
            CustomFile file = await _fileService.GetFileByIdAsync(photoId);
            byte[] bytes = await _fileService.GetFileContentAsync(file.FileKey);

            return File(bytes, file.MimeType, file.OriginalFileName ?? $"photo-{photoId}");
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}