using Microsoft.AspNetCore.Mvc;

namespace SENG302.Api.Controllers;

[ApiController]
[Route("api/geo")]
public class GeoController : ControllerBase
{
    public GeoController()
    {
        
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchLocationsAsync([FromQuery] string query)
    {
        
    }
}