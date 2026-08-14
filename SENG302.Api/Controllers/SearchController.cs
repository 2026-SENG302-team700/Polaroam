using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    
    private readonly ITripService _tripService;
    private readonly IImageService _imageService;
    private readonly IFileService _fileService;
    
    public SearchController(
        ITripService tripService,
        IImageService imageService,
        IFileService fileService
        )
    {
        _tripService = tripService;
        _imageService = imageService;
        _fileService = fileService;
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
            var photoIds = await _imageService.GetImagesFromQueryAsync(query);

            List<PhotoMetadataResponse> photos = [];
            foreach (int photo in photoIds)
            {
                CustomFile file = await _fileService.GetFileByIdAsync(photo);
                var fileTrip = await 
                
                photos.Add(new PhotoMetadataResponse
                {
                    PhotoId = photo,
                    FileName = file.OriginalFileName,
                    MimeType = file.MimeType
                });
            }

            return Ok(photos);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }
}