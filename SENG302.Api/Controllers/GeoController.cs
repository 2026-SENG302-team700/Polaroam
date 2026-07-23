using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ApiController]
[Route("api/geo")]
public class GeoController : ControllerBase
{
    private readonly IGeoService _geoService;
    
    public GeoController(IGeoService geoService)
    {
        _geoService = geoService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchLocationsAsync(
        [FromQuery] string query, 
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage, 
        CancellationToken cancellationToken)
    {
        var results = await _geoService.GetLocationsAsync(query, acceptLanguage, cancellationToken);
        return Ok(results);
    }

    [HttpGet("location")]
    public async Task<IActionResult> GetLocationAsync([FromQuery] long osmId)
    {
        var result = await _geoService.GetLocationAsync(osmId);
        return Ok(result);
    }
}