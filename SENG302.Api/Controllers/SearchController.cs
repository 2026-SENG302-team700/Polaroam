using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    
    private readonly ITripService _tripService;
    
    public SearchController(
        ITripService tripService
        )
    {
        _tripService = tripService;
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
            var trips = await _tripService.GetTripsFromQuery(query);

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
}