using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Filters;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/trip/{tripId:int}/collaboration")]
public class CollaborationController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly ICollaborationService _collaborationService;


    public CollaborationController(ITripService tripService, ICollaborationService collaborationService)
    {
        _tripService = tripService;
        _collaborationService = collaborationService;
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
    
    /// <summary>
    /// Leave the collaboration modal
    /// </summary>
    /// <param name="tripId">The trip that user wants to leave</param>
    /// <returns>
    /// Unauthorized - If User is not logged in or is not a collaborator on the trip
    /// Bad Request - If the backend failed to remove user from the trip
    /// No content 204 - If the user is able to leave successfully
    /// </returns>
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
    /// Kick the user from a given trip
    /// </summary>
    /// <param name="tripId">The trip id of the trip to remove user as collaborator from</param>
    /// <param name="kickedUserId">The id of the user that is going to be kicked</param>
    /// <returns>
    /// Ok - If the trip and user id are valid and the user can be kicked
    /// Bad Request - If the trip and user id do not correspond to real trip or user
    /// Unauthorized - The user is unauthorized and cannot be added to the trip
    /// </returns>
    [Authorize]
    [HttpDelete("kick/{kickedUserId:int}")]
    public async Task<ActionResult<IEnumerable<CollaboratingUserResponse>>> KickUser(int tripId, int kickedUserId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized("Not logged in");
        }

        var userId = int.Parse(userIdString);
        if (!await _tripService.UserHasCollaborationAccessToTrip(userId, tripId))
        {
            return Unauthorized("User does not have access to collaborate on the trip and kick users");
        }

        List<CollaboratingUserResponse> remainingCollaborators = [];
        
        try
        {
            await _collaborationService.KickUserAsync(tripId, kickedUserId);

            remainingCollaborators = await _collaborationService.GetTripCollaborators(tripId);
        }
        catch (KeyNotFoundException e)
        {
            return BadRequest(e.Message);
        }

        return Ok(remainingCollaborators);
    }
}