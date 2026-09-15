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
            if (scrapboard.OwnerId.Contains(userId)) return Unauthorized();
            if (scrapboard == null) return NotFound("Trip has no Scrapboard");
            return Ok(scrapboard);
        }
        catch (Exception e)
        {  
            return BadRequest(e);
        }
    }
}