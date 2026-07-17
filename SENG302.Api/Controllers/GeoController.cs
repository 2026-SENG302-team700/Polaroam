using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ApiController]
[Route("api/geo")]
public class GeoController : ControllerBase
{
    private readonly IGeoService _geoService;
    
    public GeoController()
    {
        
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchLocationsAsync([FromQuery] string query)
    {
        
    }
}