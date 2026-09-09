using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ApiController]
[Route("api/scrapboard")]
public class ScrapboardController : ControllerBase
{
    private readonly IScrapboardService _scrapboardService;

    public ScrapboardController(IScrapboardService scrapboardService)
    {
        _scrapboardService = scrapboardService;
    }

    /// <summary>
    /// Gets elements from scrapboard
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns></returns>
    [HttpGet("{tripId:int}")]
    public async Task<IActionResult> GetScrapboardAsync(int tripId)
    {
        var scrapboard = true;
        return Ok(scrapboard);
    }

    /// <summary>
    /// Updates the scrapboard
    /// </summary>
    /// <returns></returns>
    [HttpPut]
    public async Task<IActionResult> UpdateScrapboardAsync([FromBody] List<> elements)
    {
        var scrapboard = elements;
        return Ok(scrapboard);
    }
}