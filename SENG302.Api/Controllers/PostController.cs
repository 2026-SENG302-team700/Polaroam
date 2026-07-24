using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Helpers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using SENG302.Api.Resources.Helpers;
using System.Dynamic;
using SQLitePCL;
namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/post")]
public class PostController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPostService _postService;
    private readonly IFileService _fileService;

    public PostController(IPostService postService, IUserService userService, IFileService fileService)
    {
        _postService = postService;
        _userService = userService;
        _fileService = fileService;
    }

    /// <summary>
    /// Fetchs all the posts for the current user
    /// </summary>
    /// <returns>All posts belonging to that user</returns>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Post>>> GetAllPosts()
    {
        // get the logged in user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        try
        {
            var posts = await _postService.GetAllPostsAsync(int.Parse(userIdString));
            return Ok(posts);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Given a post request object, create a new Post and add it to the db
    /// </summary>
    /// <param name="postRequest"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Post>> CreatePost([FromBody] NewPostRequest postRequest)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return Unauthorized();

        try
        {
            var response = await _postService.CreateNewPostAsync(postRequest);
            return Ok(response);
        }
        catch (MultipleValidationException e)
        {
            return BadRequest(new BadRequestValidationResponse
            {
                Errors = e.Errors
            });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Gets a specific post by its post id
    /// </summary>
    /// <param name="id">The desired post's id</param>
    /// <returns>The posts corresponsing to the Id</returns>
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Post>> GetPost(int id)
    {
        try
        {
            var response = await _postService.GetPostAsync(id);
            return Ok(response);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Given a request to update a post, it will send a request to update that post
    /// to the PostService
    /// </summary>
    /// <param name="postUpdates">updated values for a post</param>
    /// <returns>
    /// The updated post if ok
    /// A Bad Request if an error occured within (likely validation fail).
    /// </returns>
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Post>> UpdatePost([FromBody] UpdatePostRequest postUpdates)
    {
        var errors = new Dictionary<string, string>();
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!await _postService.UserOwnsPost(userId, postUpdates.TripId))
        {
            return Unauthorized();
        }
        try
        {
            if (errors.Count > 0)
            {
                return BadRequest(new BadRequestValidationResponse
                {
                    Errors = errors
                });
            }

            var post = await _postService.UpdatePostAsync(postUpdates);
            return Ok(post);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Takes a post id and returns all the photos as form files
    /// </summary>
    /// <param name="postId">The id of the post</param>
    /// <returns>The photos as form files</returns>
    [Authorize]
    [HttpGet("{postId:int}/images")]
    public async Task<ActionResult<List<IFormFile>>> GetPostPhotos(int postId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("You do not have authorisation to upload photos to this post!");
        }

        List<IFormFile> photos = [];

        var post = await _postService.GetPostAsync(postId);

        foreach (int photoId in post?.PhotoIds ?? [])
        {
            CustomFile file = await _fileService.GetFileByIdAsync(photoId);
            byte[] bytes = await _fileService.GetFileContentAsync(file.FileKey);
            var memoryStream = new MemoryStream(bytes);
            IFormFile formFile = new FormFile(memoryStream, 0, file.FileSize, null, file.OriginalFileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = file.MimeType
            };

            photos.Add(formFile);
        }

        return Ok(photos);
    }

    /// <summary>
    /// Controller method for uploading photos to a post
    /// </summary>
    /// <param name="files">The photo files being uploaded</param>
    /// <param name="postId">The id of the post corresponding to the photos being uploaded</param>
    /// <returns>The ids of the newly create custom files for the photos</returns>
    [Authorize]
    [HttpPut("images")]
    public async Task<ActionResult<List<int>>> UploadPostPhotos(
        [FromForm] List<IFormFile> files,
        [FromForm] string postId,
        [FromForm] string tripId
        )
    {

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("You do not have authorisation to upload photos to this post!");
        }

        var userId = int.Parse(userIdString);
        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        var ownsPost = await _postService.UserOwnsPost(userId, int.Parse(tripId));



        if (ownsPost == false)
        {
            return Unauthorized("You do not have authorisation to upload photos to this post!");
        }

        List<int> customFileIds = [];

        for (int i = 0; i < files.Count; i++)
        {
            IFormFile file = files[i];

            if (!ImageValidationTools.IsValidFilesize(file))
            {
                return BadRequest("Image too large, maximum file size is 10MB");
            }

            // implement into one big callback function later.
            if (!ImageValidationTools.IsValidMimeType(file))
            {
                return BadRequest("Invalid image, supported file types are .jpeg, .png, .svg, .gif, .webp");
            }
            var customFile = await _fileService.SaveFileAsync(file, userId);
            var customFileId = customFile.Id;
            customFileIds.Add(customFileId);
        }

        await _postService.AddPhotosToPost(customFileIds, int.Parse(postId));


        return Ok(customFileIds);
    }

}