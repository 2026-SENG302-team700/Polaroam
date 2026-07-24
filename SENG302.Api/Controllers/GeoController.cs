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
    
    /// <summary>
    /// Searches for locations based on the query string
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="acceptLanguage">language for the response</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("search")]
    public async Task<IActionResult> SearchLocationsAsync(
        [FromQuery] string query, 
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage, 
        CancellationToken cancellationToken)
    {
        var results = await _geoService.GetLocationsAsync(query, acceptLanguage, cancellationToken);
        return Ok(results);
    }
    
    /// <summary>
    /// Gets the location of a given OSM id
    /// </summary>
    /// <param name="osmId">Id for a location</param>
    /// <returns></returns>
    [HttpGet("location")]
    public async Task<IActionResult> GetLocationAsync([FromQuery] long osmId)
    {
        var result = await _geoService.GetLocationAsync(osmId);
        return Ok(result);
    }
}