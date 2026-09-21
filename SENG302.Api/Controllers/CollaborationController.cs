using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using SENG302.Api.Models.Entities;
namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/trip/{tripId:int}/collaboration")]
public class CollaborationController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly ICollaborationService _collaborationService;

    private readonly IUserService _userService;
    public CollaborationController(ITripService tripService, ICollaborationService collaborationService, IUserService userService)
    {
        _tripService = tripService;
        _collaborationService = collaborationService;
        _userService = userService;
    }

    /// <summary>
    /// Returns all collaborates 
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns></returns>
    [Authorize]
    [HttpGet]
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

    [Authorize]
    [HttpPost("leave")]
    public async Task<ActionResult> LeaveCollaboration(int tripId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized("Not Logged in");

        try
        {
            // Validation
            var userId = int.Parse(userIdString);
            if (!await _tripService.UserHasCollaborationAccessToTrip(userId, tripId))
            {
                return Unauthorized("Not a collaborator on the trip");
            }

            // Leave collaboration
            await _collaborationService.LeaveCollaboration(userId, tripId);
            return NoContent();
        }
        catch
        {
            return BadRequest("Failed to leave collaboration");
        }
    }

    /// <summary>
    /// End point for posting invite to a user to a trip
    /// </summary>
    /// <param name="tripId">The trip Id</param>
    /// <param name="request">The users email</param>
    /// <returns>
    /// OK - If the email is valid and user id is valid, then it returns OK
    /// Bad Request - If the email format is invalid
    /// Unauthorized - User is not logged in or does not have access
    /// </returns>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> InviteUser(int tripId, [FromBody] InviteCollaboratorRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new CollaborationInviteResponse
            {
                Type = "error",
                Message = "Not logged in"
            });
        }

        var userId = int.Parse(userIdString);
        if (!await _tripService.UserHasCollaborationAccessToTrip(userId, tripId))
        {
            return Unauthorized(new CollaborationInviteResponse
            {
                Type = "error",
                Message = "User does not have access to collaborate on the trip and view collaboration details"
            });
        }

        try
        {
            await _collaborationService.SendInviteAsync(request.Email, userId, tripId);
        }
        catch (EmailInvalidationException e)
        {
            return BadRequest(new CollaborationInviteResponse
            {
                Type = "error",
                Message = e.Message
            });
        }
        catch (UserInTripException e)
        {
            return BadRequest(new CollaborationInviteResponse
            {
                Type = "error",
                Message = e.Message
            });
        }
        catch (PendingInviteException e)
        {
            return BadRequest(new CollaborationInviteResponse
            {
                Type = "info",
                Message = e.Message
            });
        }
        return Ok();
    }

    /// <summary>
    /// If a user has been invited to the given trip, then the action provided in collaborationRequest
    /// will be passed to the backend to have their action performed.
    /// </summary>
    /// <param name="tripId"></param>
    /// <param name="collaborationRequest"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("action")]
    public async Task<ActionResult> ActionCollaboration(int tripId, [FromBody] CollaborationRequest collaborationRequest)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            User? user = await _userService.GetUserByIdAsync(userId);
            if (user == null ||
                !await _tripService.UserHasCollaborationAccessToTrip(user.Id, tripId)) //does not work properly yet
                return Unauthorized();
            if (collaborationRequest.Action == 0) // reject
            {
                await _collaborationService.ActionRejectAsync(tripId, user);
                return Ok("successfully rejected");
            }
            else if (collaborationRequest.Action == 1) //accept
            {
                await _collaborationService.ActionAcceptAsync(tripId, user);
                return Ok("successfully accepted");
            }
            else //invalid action
            {
                return BadRequest("Not a valid collaboration action!");
            }

        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}