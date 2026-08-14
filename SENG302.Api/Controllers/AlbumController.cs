using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/album/{tripId:int}")]
public class AlbumController : ControllerBase
{
    private readonly IAlbumService _albumService;
    private readonly IUserService _userService;
    private readonly ITripService _tripService;
    private readonly IFileService _fileService;
    
    public AlbumController(IAlbumService albumService, IUserService userService, ITripService tripService, IFileService fileService)
    {
        _albumService = albumService;
        _userService = userService;
        _tripService = tripService;
        _fileService = fileService;
    }
    
    /// <summary>
    /// Helper function to get the content type of a file.
    /// </summary>
    /// <param name="fileKey"></param>
    /// <returns>The contentType string</returns>
    private static string GetContentType(string fileKey)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileKey, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }
    
    /// <summary>
    /// Gets an image from an album.
    /// </summary>
    /// <param name="tripId"></param>
    /// <param name="fileKey"></param>
    /// <returns>
    /// Ok - image found
    /// Unauthorized - user not logged in or does not have access to the trip
    /// NotFound - image/album not found
    /// BadRequest - unhandled error
    /// </returns>
    [HttpGet("images/{fileKey}")]
    public async Task<ActionResult> GetAlbumImage(int tripId, string fileKey)
    {
        try
        {
            // Get user and return unauthorized if not logged in or access to the trip
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null ||
                !await _tripService.UserHasModifyAccessToTrip(user.Id, tripId))
                return Unauthorized();

            // Get the image
            var image = await _fileService.GetFileContentAsync(fileKey);
            var contentType = GetContentType(fileKey);
            return File(image, contentType);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Image not found");
        }
        catch (Exception)
        {
            return BadRequest("Failed to get image: " + fileKey);
        }
    }
    
    /// <summary>
    /// Gets all images from an album. This includes images added
    /// directly to the album and images added to a post in the trip.
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns>List of images</returns>
    [HttpGet]
    public async Task<ActionResult> GetAlbumImageMetadata(int tripId)
    {
        try
        {
            // Get user and return unauthorized if not logged in or access to the trip
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null ||
                !await _tripService.UserHasModifyAccessToTrip(user.Id, tripId))
                return Unauthorized();

            var images = await _albumService.GetImagesMetadataFromAlbumAsync(tripId);
            return Ok(images);
        }
        catch (KeyNotFoundException e) // Trip or Album not found
        {
            return NotFound(e.Message);
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
    [HttpPost("images")]
    public async Task<ActionResult> AddImagesToAlbum(int tripId, [FromForm] List<IFormFile>? files)
    {
        try
        {
            // Get user and return unauthorized if not logged in or access to the trip
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null ||
                !await _tripService.UserHasModifyAccessToTrip(user.Id, tripId))
                return Unauthorized();

            if (files == null) return BadRequest("No files were uploaded.");

            // Upload images to the album
            await _albumService.UploadImagesToAlbumAsync(files, tripId);
            return Created();
        }
        catch (MultipleValidationListException e) // Image validation failed
        {
            return BadRequest(e.Errors);
        }
        catch (KeyNotFoundException e) // Album or Trip not found
        {
            return NotFound(e.Message);
        }
        catch (Exception e) // Other exceptions
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
    [HttpDelete("images/{imageId:int}")]
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

            var postId = await _albumService.DeleteImageFromAlbumAsync(imageId, tripId);
            if (postId != null) return StatusCode(405, postId); // Return NotAllowed if the image is a part of a post with the post-ID
            return NoContent(); // Success
        }
        catch (KeyNotFoundException e) // Image not found, or album not found
        {
            return NotFound(e.Message);
        }
        catch (Exception)
        {
            return BadRequest("Failed to delete image");
        }
    }
}