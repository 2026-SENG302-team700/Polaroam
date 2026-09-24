using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
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
    public async Task<ActionResult<IEnumerable<PostResponse>>> GetAllPosts()
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
            var postResponses = new List<PostResponse>();

            foreach (Post post in posts)
            {
                postResponses.Add(new PostResponse(post, await _userService.GetUserDisplayName(post.CreatorId)));
            }

            return Ok(postResponses);
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
    /// <returns>Returns the id of the new post</returns>
    [Authorize]
    [HttpPost]
    [RequestSizeLimit(200000000)]
    public async Task<ActionResult<int>> CreatePost([FromForm] NewPostRequest postRequest, [FromForm] List<IFormFile>? files, [FromForm] bool aiTaggingForUpload = true)
    {

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        User? user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return Unauthorized();

        try
        {
            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, postRequest.TripId)) return NotFound();

            var response = await _postService.CreateNewPostAsync(postRequest, userId, files, aiTaggingForUpload);
            return Ok(response.PostId);
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
    public async Task<ActionResult<PostResponse>> GetPost(int id)
    {
        try
        {
            // verify the logged in user owns the post
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var post = await _postService.GetPostAsync(id);

            if (post == null || !await _tripService.UserHasCollaborationAccessToTrip(userId, post.TripId)) return NotFound();

            var postResponse = new PostResponse(post, await _userService.GetUserDisplayName(post.CreatorId));

            return Ok(postResponse);
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
    /// The id of the updated post if ok
    /// A Bad Request if an error occured within (likely validation fail).
    /// </returns>
    [Authorize]
    [HttpPut("{id:int}")]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<int>> UpdatePost(
        [FromForm] UpdatePostRequest postUpdates,
        [FromForm] List<IFormFile>? files,
        [FromForm] bool aiTaggingForUpload = true)
    {
        // verify the user owns the post first
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var post = await _postService.GetPostAsync(postUpdates.Id);

        if (post == null) return NotFound();

        if (!await _tripService.UserHasCollaborationAccessToTrip(userId, post.TripId)!) return NotFound();

        try
        {
            post = await _postService.UpdatePostAsync(postUpdates, userId, files, aiTaggingForUpload);
            return Ok(post?.PostId);
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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var post = await _postService.GetPostAsync(postId);
        if (post == null || !await _tripService.UserHasCollaborationAccessToTrip(userId, post.TripId))
        {
            return NotFound("Post not found");
        }

        List<PhotoMetadataResponse> photos = [];
        foreach (int photoId in post.PhotoIds ?? [])
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

        if (!await _postService.UserOwnsPost(userId, post.TripId, id))
        {
            return NotFound();
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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var post = await _postService.GetPostAsync(postId);

        if (post == null)
        {
            return NotFound("Post does not exist");
        }

        if (!await _tripService.UserHasCollaborationAccessToTrip(userId, post.TripId))
        {
            return NotFound();
        }

        // check the photo belongs to the post
        if (post.PhotoIds == null || !post.PhotoIds.Contains(photoId))
        {
            return NotFound("Photo does not belong to this post");
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
