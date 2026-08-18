using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{

    private readonly ITripService _tripService;
    private readonly IImageService _imageService;

    public SearchController(
        ITripService tripService,
        IImageService imageService
        )
    {
        _tripService = tripService;
        _imageService = imageService;
    }
    
    [HttpGet("my-trips")]
    public async Task<IActionResult> SearchMyTripsAsync([FromQuery] string query)
    {
        // get the user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        try
        {
            var trips = await _tripService.GetTripsFromQuery(int.Parse(userIdString), query);

            if (trips.Count > 0)
            {
                return Ok(trips);
            }

            return NotFound();
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpGet("photos")]
    public async Task<IActionResult> SearchMyPhotosAsync([FromQuery] string query)
    {
        // get the user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        try
        {
            var tripPhotos = await _imageService.GetPhotosFromQueryAsync(int.Parse(userIdString), query);

            if (tripPhotos.Count > 0)
            {
                return Ok(tripPhotos);
            }

            return NotFound();
        }
        catch
        {
            return BadRequest();
        }
    }
}