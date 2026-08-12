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
    /// Gets all images from an album. This includes images added
    /// directly to the album and images added to a post in the trip.
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns>List of images</returns>
    [HttpGet("{tripId:int}")]
    public async Task<ActionResult> GetImagesFromAlbum(int tripId)
    {
        try
        {
            // Get user and return unauthorized if not logged in or access to the trip
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null ||
                !await _tripService.UserHasModifyAccessToTrip(user.Id, tripId))
                return Unauthorized();
            
            var images = await _albumService.GetImagesFromAlbumAsync(tripId);
            return Ok(images);
        }
        catch
        {
            return BadRequest();
        }
    }
    
    /// <summary>
    /// Adds images to an album.
    /// </summary>
    /// <param name="tripId">The trips id</param>
    /// <param name="files">files being uploaded to the album</param>
    /// <returns>Http response based on the result:
    /// Created - upload successful
    /// BadRequest - validation failed
    /// Unauthorized - user not logged in or does not have access to the trip
    /// NotFound - trip not found
    /// </returns>
    [HttpPost("{tripId:int}/images")]
    public async Task<ActionResult> AddImagesToAlbum(int tripId, [FromForm] List<IFormFile>? files)
    {
        try
        {
            // Get user and return unauthorized if not logged in or access to the trip
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null ||
                ! await _tripService.UserHasModifyAccessToTrip(user.Id, tripId))
                return Unauthorized();
            
            // Upload images to the album
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
    
    /// <summary>
    /// Deletes an image from an album.
    /// </summary>
    /// <param name="tripId">the trip the album is a part of</param>
    /// <param name="imageId">The image to be deleted</param>
    /// <returns>
    /// NoContent - image deleted
    /// Unauthorized - user not logged in or does not have access to the trip.
    /// NotAllowed - image is a part of a post and cannot be deleted (return post-ID as well).
    /// BadRequest - validation failed (With reason or generic on error).
    /// </returns>
    [HttpDelete("{tripId:int}/images/{imageId:int}")]
    public async Task<ActionResult> DeleteAlbumImage(int tripId, int imageId)
    {
        try
        {
            // Get user and return unauthorized if not logged in or access to the trip
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null ||
                !await _tripService.UserHasModifyAccessToTrip(user.Id, tripId))
                return Unauthorized();

            await _albumService.DeleteImageFromAlbumAsync(tripId, imageId);
            return NoContent();
        }
        catch (ArgumentException e) // Handled Exceptions
        {
            return BadRequest(e.Message);
        }
        catch (InvalidOperationException e) // Returned when the image is a part of a post and include the post-ID
        {
            return StatusCode(405, int.Parse(e.Message));
        }
        catch (Exception)
        {
            return BadRequest("Failed to delete image.");
        }
    }
}