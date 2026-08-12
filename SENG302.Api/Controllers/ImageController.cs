using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/post")]
public class ImageController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPostService _postService;
    private readonly IFileService _fileService;
    private readonly ITripService _tripService;

    public ImageController(IPostService postService, IUserService userService, IFileService fileService, ITripService tripService)
    {
        _postService = postService;
        _userService = userService;
        _fileService = fileService;
        _tripService = tripService;

    }

    /// <summary>
    /// Returns the tags of the requests photo
    /// </summary>
    /// <param name="photoId">id of photo</param>
    /// <returns>an array of strings containing the image tags</returns>
    [Authorize]
    [HttpGet("{postId:int}/images/{photoId:int}/tags")]
    public async Task<IActionResult> GetPhotoTags(int photoId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if (user == null || await _fileService.CheckFileOwnership(photoId, userId))
        { 
            return Unauthorized();
        }

        try
        {
            var tags = _fileService.GetTagsAsync(photoId);
            return Ok(tags);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}