using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/scrapboard")]
public class ScrapboardController : ControllerBase
{
    private readonly IAlbumService _albumService;
    private readonly IUserService _userService;
    private readonly ITripService _tripService;
    private readonly IFileService _fileService;
    private readonly IScrapboardService _scrapboardService;

    public ScrapboardController(IScrapboardService scrapboardService, IAlbumService albumService, IUserService userService, ITripService tripService, IFileService fileService)
    {
        _albumService = albumService;
        _userService = userService;
        _tripService = tripService;
        _fileService = fileService;
        _scrapboardService = scrapboardService;
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var scrapboard = await _scrapboardService.GetScrapboardByTripAsync(tripId);
            if (scrapboard == null) return NotFound("Trip has no Scrapboard");
            if (scrapboard.OwnerId != userId) return Unauthorized();

            return Ok(scrapboard);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var trip = await _tripService.GetTripByIdAsync(request.TripId);
            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, trip.Id))
            {
                return Unauthorized();
            }
            var scrapboard = await _scrapboardService.CreateScrapboardAsync(request.TripId, userId);

            return Ok(scrapboard);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}

