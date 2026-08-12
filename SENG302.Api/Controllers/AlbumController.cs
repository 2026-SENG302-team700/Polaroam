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
[Route("api/album")]
public class AlbumController : ControllerBase
{
    private readonly IAlbumService _albumService;
    private readonly IUserService _userService;
    private readonly ITripService _tripService;
    
    public AlbumController(IAlbumService albumService, IUserService userService, ITripService tripService)
    {
        _albumService = albumService;
        _userService = userService;
        _tripService = tripService;
    }
    
    /// <summary>
    /// Adds images to an album.
    /// </summary>
    /// <param name="tripId">The trips id</param>
    /// <param name="files">files being uploaded to the album</param>
    /// <returns>Http response based on result:
    /// Created - upload successful
    /// BadRequest - validation failed
    /// Unauthorized - user not logged in or does not have access to the trip
    /// NotFound - trip not found
    /// </returns>
    [HttpPost("{tripId:int}/images")]
    public async Task<ActionResult> AddImagesToAlbum(int tripId, [FromForm] List<IFormFile>? files)
    {
        // Get user and return unauthorized if not logged in or access to the trip
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if (user == null ||
            ! await _tripService.UserHasModifyAccessToTrip(user.Id, tripId))
            return Unauthorized();
        
        // Upload images to the album
        try
        {
            await _albumService.UploadImagesToAlbumAsync(files, tripId, user.Id);
            return Created();
        }
        catch (ValidationListException e) // Image validation failed
        {
            return BadRequest(e.Errors);
        }
        catch (ArgumentException e) // Trip not found
        {
            return NotFound(e.Message);
        }
        catch (Exception) // Other exceptions
        {
            return BadRequest("Failed to upload images.");
        }
        
    }
}