using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/image-tags")]
public class ImageTaggingController : ControllerBase
{
    private readonly IImageTaggingService _imageTaggingService;

    public ImageTaggingController(
        IImageTaggingService imageTaggingService
    )
    {
        _imageTaggingService = imageTaggingService;
    }

    /// <summary>
    /// Post endpoint for generating the tags for images.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> GenerateTags([FromForm] IFormFile image, CancellationToken cancellationToken)
    {
        try
        {
            /*
            Future queue:
                Save image, Enqueue its file id, return 202, Background worker calls GenerateTagsAsync
            */
            // The call below is only for the spike
            var tags = await _imageTaggingService.GenerateTagsAsync(image, cancellationToken);

            return Ok(tags);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "The image-tagging service could not be reached.");
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, exception.Message);
        }
    }
}