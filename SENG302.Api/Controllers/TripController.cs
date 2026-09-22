using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
    /// <returns>
    /// trip with the Given id
    /// Unauthorized if the user is not signed in or does not own the trip
    /// Not Found if no trip exists with the given id
    /// </returns>
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TripResponse>> GetTrip(int id)
    {
        // get the user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Not logged in!");
        }
        var userId = int.Parse(userIdString);

        try
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, id))
                return NotFound("Trip not found");
            return Ok(await TripToTripResponse(trip));
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
    public async Task<ActionResult<UserResponse>> GetTripOwnerName(int tripId)
    {
        // get the user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Not logged in!");
        }
        var userId = int.Parse(userIdString);

        var trip = await _tripService.GetTripByIdAsync(tripId);
        var ownerUser = await _userService.GetUserByIdAsync(trip.UserId);
        if (ownerUser == null || !await _tripService.UserHasCollaborationAccessToTrip(userId, trip.Id))
        {
            return NotFound();
        }

        var response = new UserResponse
        {
            Id = ownerUser.Id,
            Email = ownerUser.Email,
            DisplayName = ownerUser.DisplayName,
            Country = ownerUser.Country
        };
        return Ok(response);
    }

    /// <summary>
    /// Gets all trips associated with the logged-in user
    /// </summary>
    /// <returns>
    /// Array of trips, empty if none exist
    /// Unauthorized if not logged in
    /// Ok on success
    /// </returns>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripResponse>>> GetTripsForUser()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            return Unauthorized("Not logged in!");

        var trips = await _tripService.GetTripsByUserIdAsync(int.Parse(userIdString));

        var tripResponses = new List<TripResponse>();
        foreach (Trip trip in trips)
        {
            tripResponses.Add(await TripToTripResponse(trip));
        }

        return Ok(tripResponses);
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
    public async Task<ActionResult<TripResponse>> CreateTrip([FromBody] NewTripRequest tripRequest)
    {
        try
        {
            // Check user is logged in
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized("Not logged in!");

            // Create trip belonging to the logged in user
            var trip = await _tripService.CreateNewTripAsync(tripRequest, int.Parse(userIdString));

            await _albumService.CreateNewAlbumAsync(trip.Id);

            return Ok(trip);
        }
        catch (MultipleValidationListException e)
        {
            return BadRequest(new BadRequestValidationListResponse
            {
                Errors = e.Errors
            });
        }
        catch (KeyNotFoundException e)
        {
            return Problem(e.Message);
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
    public async Task<ActionResult> DeleteTrip(int id)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Not logged in!");
            }

            if (!await _tripService.UserIsOwnerOfTrip(int.Parse(userIdString), id))
            {
                return NotFound("Trip not found");
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
    public async Task<ActionResult<TripResponse>> UpdateTrip([FromBody] UpdateTripRequest tripRequest)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized("Not logged in!");
            var userId = int.Parse(userIdString);
            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, tripRequest.Id))
                return Unauthorized("You do not have permission to edit this trip!");
            var trip = await _tripService.UpdateTripAsync(tripRequest, int.Parse(userIdString));
            if (trip == null)
            {
                return NotFound();
            }
            return Ok(await TripToTripResponse(trip));
        }
        catch (MultipleValidationListException exception)
        {
            return BadRequest(new BadRequestValidationListResponse
            {
                Errors = exception.Errors
            });
        }
        catch
        {
            return BadRequest("Failed to update trip");
        }
    }

    /// <summary>
    /// Fetches all posts associated to the id of the given trip. If
    /// no trip is provided, then throw a BadRequest Error.
    /// </summary>
    /// <param name="tripId">Default value of -1, referencing a trip</param>
    /// <returns>The list of tasks</returns>
    [Authorize]
    [HttpGet("post/{tripId:int}")]
    public async Task<ActionResult<IEnumerable<Post>>> GetPostsFromTrip(int tripId = -1)
    {
        // Check tripId has been provided
        if (tripId < 0)
        {
            return BadRequest("Trip not provided");
        }

        // Check logged in
        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Not logged in!");
        }

        // Check has access to the trip
        var userId = int.Parse(userIdString);
        if (!await _tripService.UserHasCollaborationAccessToTrip(userId, tripId)) return Unauthorized("You are not allowed to access this Trip's Posts!");

        // Get list of posts
        var posts = await _postService.GetPostsByTripAsync(tripId);
        return Ok(posts);
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
        // Check logged in
        string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Not logged in!");
        }
        if (!await _tripService.UserHasCollaborationAccessToTrip(int.Parse(userIdString!), tripId))
        {
            return NotFound("Trip not found");
        }
        var locations = await _tripService.GetTripLocations(tripId);
        return Ok(locations);
    }

    /// <summary>
    /// Returns all collaborates 
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("{tripId:int}/collaborators")]
    public async Task<ActionResult<IEnumerable<CollaboratingUserResponse>>> GetTripCollaborators(int tripId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
            return Unauthorized();

        try
        {
            if (int.TryParse(userIdString, out var userId))
            {
                if (!await _tripService.UserHasCollaborationAccessToTrip(userId, tripId))
                {
                    return Unauthorized(
                        "User does not have access to collaborate on the trip and view collaboration details");
                }
            }
            else
            {
                return Unauthorized("User Id does not exist");
            }

            var collaborators = await _collaborationService.GetTripCollaborators(tripId);
            return Ok(collaborators);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ArgumentException)
        {
            return NotFound("This trip does not exist");
        }
    }

    public async Task<TripResponse> TripToTripResponse(Trip trip)
    {
        var user = await _userService.GetUserByIdAsync(trip.UserId) ?? throw new KeyNotFoundException("User not found");

        var tripResponse = new TripResponse
        {
            Id = trip.Id,
            Title = trip.Title,
            UserId = trip.UserId,
            Description = trip.Description,
            EndDate = trip.EndDate,
            StartDate = trip.StartDate,
            OwnerName = user.DisplayName,
            Collaborators = await _collaborationService.GetTripCollaborators(trip.Id),
        };
        return tripResponse;
    }

}