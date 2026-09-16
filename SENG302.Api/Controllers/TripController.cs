using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/trip")]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IPostService _postService;
    private readonly IAlbumService _albumService;
    private readonly IUserService _userService;
    private readonly ICollaborationService _collaborationService;


    public TripController(ITripService tripService, IPostService postService, IAlbumService albumService, IUserService userService, ICollaborationService collaborationService)
    {
        _tripService = tripService;
        _postService = postService;
        _albumService = albumService;
        _userService = userService;
        _collaborationService = collaborationService;
    }

    /// <summary>
    /// Gets a trip by its ID. Returns 404 if no trip with the given ID exists.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Trip>> GetTrip(int id)
    {
        // get the user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        try
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            return Ok(trip);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Get the user based on trip id
    /// </summary>
    /// <param name="tripId">id for the trip</param>
    /// <returns>user display name</returns>
    [Authorize]
    [HttpGet("{tripId:int}/user")]
    public async Task<ActionResult<GetUserResponse>> GetTripOwnerName(int tripId)
    {
        var trip = await _tripService.GetTripByIdAsync(tripId);

        var user = await _userService.GetUserByIdAsync(trip.UserId);

        if (user == null)
        {
            return NotFound("User not found");
        }
        return Ok(user.DisplayName);
    }

    /// <summary>
    /// Gets all trips associated with a user's email. Returns an empty array if no trips are found for the given email.
    /// </summary>
    /// <param name="userEmail"></param>
    /// <returns></returns>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Trip>>> GetTripsForUser()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            return Unauthorized();


        var trips = await _tripService.GetTripsByUserIdAsync(int.Parse(userIdString));
        return Ok(trips);
    }

    /// <summary>
    /// Creates a new trip for a user with the given email and name. 
    /// Validates the name and user email before creating the trip.
    /// Returns 400 if validation fails, or 200 if the trip is created successfully.
    /// </summary>
    /// <param name="tripRequest"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Trip>> CreateTrip([FromBody] NewTripRequest tripRequest)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();
            var trip = await _tripService.CreateNewTripAsync(tripRequest, int.Parse(userIdString));

            var album = await _albumService.CreateNewAlbumAsync(trip.Id);

            return Ok(trip);
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
    /// Deletes a trip based on the id provided.
    /// </summary>
    /// <param name="id">The id of the trip to delete.</param>
    /// <returns>
    /// 400 if the trip does not exist.
    /// 200 if the trip is deleted successfully.
    /// </returns>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Trip>> DeleteTrip(int id)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            if (!await _tripService.UserHasCollaborationAccessToTrip(int.Parse(userIdString), id))
            {
                return Unauthorized();
            }

            await _tripService.DeleteTripByIdAsync(id);
            await _postService.DeleteAllPostOfTrip(id);
            return Ok();
        }
        catch (ArgumentException)
        {
            return BadRequest("Trip does not exist");
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    /// <summary>
    /// Updates an existing trip with the given ID and new details.
    /// Validates the new details before updating the trip.
    /// Returns 404 if no trip with the given ID exists or 200 if the trip is updated successfully.
    /// Returns 400 if validation fails.
    /// </summary>
    /// <param name="tripRequest">Request DTO containing all the updated trip details</param>
    /// <returns></returns>
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Trip>> UpdateTrip([FromBody] UpdateTripRequest tripRequest)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();

            var trip = await _tripService.UpdateTripAsync(tripRequest, int.Parse(userIdString));
            if (trip == null)
            {
                return NotFound();
            }
            return Ok(trip);
        }
        catch (MultipleValidationListException exception)
        {
            return BadRequest(new BadRequestValidationListResponse
            {
                Errors = exception.Errors
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// Fetches all posts associated to the id of the given trip. If
    /// no trip is provided, then throw a BadRequest Error.
    /// </summary>
    /// <param name="tripId = -1"></param>
    /// <returns>The list of tasks</returns>
    [Authorize]
    [HttpGet("post/{tripId:int}")]
    public async Task<ActionResult<IEnumerable<Post>>> GetPostsFromTrip(int tripId = -1)
    {
        if (tripId < 0)
        {
            return BadRequest("Trip not provided");
        }
        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("You are not allowed to access this Trip's Posts!");
        }
        var taskList = await _postService.GetPostsByTripAsync(tripId);
        return Ok(taskList);
    }

    /// <summary>
    /// Gets all locations associated with a trip.
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns>A list of location objects</returns>
    [Authorize]
    [HttpGet("locations/{tripId:int}")]
    public async Task<ActionResult<IEnumerable<Location>>> GetPostLocations(int tripId)
    {
        var locations = await _tripService.GetTripLocations(tripId);
        return Ok(locations);
    }
    

}