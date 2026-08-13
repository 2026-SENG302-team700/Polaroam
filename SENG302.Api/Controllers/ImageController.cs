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


    public ImageController(
        IUserService userService, 
        IFileService fileService, 
        IImageTaggingQueue imageTaggingQueue
    )
    {
        _userService = userService;
        _fileService = fileService;
        _imageTaggingQueue = imageTaggingQueue;
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

                var error = await _fileService.GetTagErrors(photoId);
                
                if (error != null)
                {
                    return Problem(detail: error,
                        statusCode: 500);
                }
                
                var imageInQueue = await _fileService.GetTagQueueStatus(photoId);
                return imageInQueue
                    ? StatusCode(StatusCodes.Status202Accepted)
                    : StatusCode(StatusCodes.Status204NoContent);
            }
            return Ok(tags);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
     
}