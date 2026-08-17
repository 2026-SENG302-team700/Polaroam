using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using SENG302.Api.Models.Responses;
using SENG302.Api.Helpers.Validators;
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
    private readonly ITripService _tripService;

    public PostController(IPostService postService, IUserService userService, IFileService fileService, ITripService tripService)
    {
        _postService = postService;
        _userService = userService;
        _fileService = fileService;
        _tripService = tripService;

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
    /// <param name="files"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost]
    [RequestSizeLimit(200000000)]
    public async Task<ActionResult<Post>> CreatePost([FromForm] NewPostRequest postRequest, [FromForm] List<IFormFile>? files)
    {
        
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return Unauthorized();

        try
        {
            var response = await _postService.CreateNewPostAsync(postRequest, userId, files);
            return Ok(response);
        }
        catch (MultipleValidationListException e)
        {
            return BadRequest(new BadRequestValidationListResponse
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
            var post = await _postService.GetPostAsync(id);
            if (post == null) return NotFound();
            return Ok(post);
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
    public async Task<ActionResult<Post>> UpdatePost(
        [FromForm] UpdatePostRequest postUpdates,
        [FromForm] List<IFormFile>? files)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);

        if (user == null || !await _postService.UserOwnsPost(userId, postUpdates.TripId))
        {
            return Unauthorized();
        }

        try
        {
            var post = await _postService.UpdatePostAsync(postUpdates, userId, files);
            return Ok(post);
        }
        catch (MultipleValidationListException e)
        {
            return BadRequest(new BadRequestValidationListResponse
            {
                Errors = e.Errors
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                "Post does not exist"
            );
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

    }

    /// <summary>
    /// Takes a post id and returns all the metadata related to the photos
    /// </summary>
    /// <param name="postId">The id of the post</param>
    /// <returns>a list of metadata objects</returns>
    [Authorize]
    [HttpGet("{postId:int}/images")]
    public async Task<ActionResult<List<PhotoMetadataResponse>>> GetPostPhotos(int postId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("You do not have authorisation to view the images in this post");
        }

        var post = await _postService.GetPostAsync(postId);
        if (post == null)
        {
            return NotFound("Post not found");
        }

        List<PhotoMetadataResponse> photos = [];
        foreach(int photoId in post.PhotoIds ?? [])
        {
            CustomFile file = await _fileService.GetFileByIdAsync(photoId);
            photos.Add(new PhotoMetadataResponse
            {
                PhotoId = photoId,
                FileName = file.OriginalFileName ?? $"photo-{photoId}",
                MimeType = file.MimeType,
                Tags = file.Tags,
                DateCreated = file.DateCreated
            });
        }

        return Ok(photos);
    }


    /// <summary>
    /// Streams a single photo back with correct types
    /// </summary>
    /// <param name="postId">post Id</param>
    /// <param name="photoId">id of photo</param>
    /// <returns>an image file</returns>
    [Authorize]
    [HttpGet("{postId:int}/images/{photoId:int}")]
    public async Task<IActionResult> GetPostPhoto(int postId, int photoId)
    {
        var post = await _postService.GetPostAsync(postId);
        if (post == null)
        {
            return NotFound("Post not found");
        }

        if (post.PhotoIds is null || !post.PhotoIds.Contains(photoId))
        {
            return NotFound("Photo not found on this post.");
        }

        CustomFile file = await _fileService.GetFileByIdAsync(photoId);
        byte[] bytes = await _fileService.GetFileContentAsync(file.FileKey);

        return File(bytes, file.MimeType, file.OriginalFileName ?? $"photo-{photoId}");
    }

    /// <summary>
    /// Returns the users display name from the post id so that it can be displayed with a trip
    /// </summary>
    /// <param name="postId"></param>
    /// <returns></returns>
    [HttpGet("{postId:int}/user")]
    public async Task<ActionResult<User>> GetUserNameByPost(int postId)
    {
        var post = await _postService.GetPostAsync(postId);

        if (post == null)
        {
            return NotFound("Post not found!");
        }
        var tripId = post.TripId;

        var trip = await _tripService.GetTripByIdAsync(tripId);

        var user = await _userService.GetUserByIdAsync(trip.UserId);

        if (user == null)
        {
            return NotFound("User not found");
        }
        return Ok(user.DisplayName);
    }

    /// <summary>
    /// Endpoint for removing a specific post by its id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        var post = await _postService.GetPostAsync(id);

        if (post == null)
        {
            return NotFound("Post does not exist");
        }

        var userId = int.Parse(userIdString);

        if (!await _postService.UserOwnsPost(userId, post.TripId))
        {
            return Unauthorized();
        }

        try
        {
            await _postService.DeletePostByIdAsync(id);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [Authorize]
    [HttpDelete("{postId:int}/images/{photoId:int}")]
    public async Task<IActionResult> DeletePhotoFromPost(int postId, int photoId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        var post = await _postService.GetPostAsync(postId);

        if (post == null)
        {
            return NotFound("Post does not exist");
        }

        var userId = int.Parse(userIdString);

        if (!await _postService.UserOwnsPost(userId, post.TripId))
        {
            return Unauthorized();
        }

        try
        {
            await _postService.DeletePhotoFromPostAsync(postId, photoId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}