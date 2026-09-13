using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Services;

namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/scrapboard/{tripId:int}")]
public class ScrapboardController : ControllerBase
{
    private readonly IAlbumService _albumService;
    private readonly IUserService _userService;
    private readonly ITripService _tripService;
    private readonly IFileService _fileService;

    public ScrapboardController(IAlbumService albumService, IUserService userService, ITripService tripService, IFileService fileService)
    {
        _albumService = albumService;
        _userService = userService;
        _tripService = tripService;
        _fileService = fileService;
    }
}