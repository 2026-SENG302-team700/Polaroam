using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/scrapboard")]
public class ScrapboardController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IScrapboardService _scrapboardService;
    private readonly IFileService _fileService;

    public ScrapboardController(IScrapboardService scrapboardService, ITripService tripService, IFileService fileService)
    {
        _tripService = tripService;
        _scrapboardService = scrapboardService;
        _fileService = fileService;
    }


    /// <summary>
    /// given a tripid, checks if the user is permitted to access this scrapboard,
    /// and if the trip actually has a scrapboard. 
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns>the scrapboard db object</returns>
    [Authorize]
    [HttpGet("{tripId:int}")]
    public async Task<ActionResult<Scrapboard>> GetScrapboard(int tripId)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Not logged in");
            }
            var userId = int.Parse(userIdString);
            
            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, tripId))
            {
                return Unauthorized();
            }
            
            var scrapboard = await _scrapboardService.GetScrapboardByTripAsync(tripId);
            if (scrapboard == null) return NotFound("Trip has no Scrapboard");

            var files = new List<ScrapboardFileDto>();
            foreach (var fileId in scrapboard.Files)
            {
                var customFile = await _fileService.GetFileByIdAsync(fileId);
                var mimeType = await _fileService.GetMimeTypeAsync(fileId);
                var content = await _fileService.GetFileContentAsync(customFile.FileKey);

                var excalidrawId = Path.GetFileNameWithoutExtension(customFile.OriginalFileName);
                files.Add(new ScrapboardFileDto(excalidrawId, mimeType, content));
            }
            
            return Ok(new ScrapboardDto(scrapboard.Elements, files));
        }
        catch (Exception e)
        {  
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Gets the thumbnail of a scrapboard
    /// </summary>
    /// <param name="tripId">The trip id associated with the scrapboard</param>
    /// <returns>
    /// Unauthorized - User not logged in or not associated with the scrapboard
    /// Bad Request - Trip/Scrapboard does not exist
    /// Ok - Returns the scrapboard thumbnail
    /// </returns>
    [HttpGet("{tripId:int}/thumbnail")]
    public async Task<ActionResult> GetScrapboardThumbnail(int tripId)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Not logged in");
            }

            var userId = int.Parse(userIdString);

            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, tripId))
            {
                return Unauthorized();
            }

            var thumbnail = await _scrapboardService.GetScrapboardThumbnailAsync(tripId);
            var thumbnailContent = await _fileService.GetFileContentAsync(thumbnail.FileKey);
            var thumbnailMime = await _fileService.GetMimeTypeAsync(thumbnail.Id);

            return File(thumbnailContent, thumbnailMime);
        }
        catch (KeyNotFoundException e)
        {
            return BadRequest(e.Message);
        }
        catch (FileNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    /// <summary>
    /// given a scrapboard request, creates a new scrapboard, assuming the user
    /// is authorised to create the scrapboard for this trip, as well as ensuring 
    /// the trip exists.
    /// </summary>
    /// <param name="request"></param>
    /// <returns>the scrapboard back to the frontend</returns>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Scrapboard>> CreateNewScrapboard([FromBody] NewScrapboardRequest request)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Not logged in");
            }
            var userId = int.Parse(userIdString);
            
            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, request.TripId))
            {
                return Unauthorized();
            }
            
            var scrapboard = await _scrapboardService.CreateScrapboardAsync(request.TripId);
            
            return Ok(scrapboard);
        }
        catch (Exception e)
        {  
            return BadRequest(e.Message);
        }
    }
    
    /// <summary>
    /// Takes scrapboard data and saves it into the database
    /// </summary>
    /// <param name="request">Made up of the trip id, scrapboard elements, associated files (images) and a generated thumbnail</param>
    /// <returns>
    /// Unauthorized - User is not logged in or does not have access to the trip
    /// Bad Request - Thumbnail is too large or incorrect mime
    /// Ok - Scrapboard DB object updated sucessfully
    /// </returns>
    [HttpPut("save")]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult> SaveScrapboard([FromForm] SaveScrapboardRequest request)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Not logged in");
            }

            var userId = int.Parse(userIdString);

            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, request.TripId))
            {
                return Unauthorized();
            }

            await _scrapboardService.SaveScrapboardAsync(
                request.TripId,
                request.Elements,
                request.Files,
                request.Thumbnail
            );
            
            return Ok();
        }
        catch (FileSizeExceededException e)
        {
            return BadRequest(e.Message);
        }
        catch (FileMimeTypeInvalidException e)
        {
            return BadRequest(e.Message);
        }
        catch (KeyNotFoundException e)
        {
            return BadRequest(e.Message);
        }
    }
}

