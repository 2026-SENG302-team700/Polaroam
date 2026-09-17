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
[Route("api/user/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Fetch a specific count of notifications for the logged in user
    /// </summary>
    /// <param name="count">The count of notifications to fetch</param>
    /// <returns>list of notifications</returns>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<GetAllNotificationsResponse>> GetAllNotifications([FromQuery] int? count)
    {
        // get the logged in user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        try
        {
            var userId = int.Parse(userIdString);
            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, count ?? 5);
            List<NotificationResponse> notificationResponses = [.. notifications.Select(n => new NotificationResponse(n))];
            var unreadCount = await _notificationService.GetUnreadCount(userId);
            
            return Ok(new GetAllNotificationsResponse() { NotificationResponses = notificationResponses, UnreadCount = unreadCount });
        }
        catch (FormatException)
        {
            return Unauthorized("Invalid User Id");
        }
        catch (OverflowException)
        {
            return Unauthorized("Invalid User Id");
        }
        catch (Exception)
        {
            return Problem();
        }
    }

    /// <summary>
    /// Sets the read status of a notification
    /// </summary>
    /// <param name="id">id of notification to update</param>
    /// <param name="status">new status of notification</param>
    /// <returns></returns>
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult> SetReadStatus([FromRoute] int id, [FromBody] bool status)
    {
        // get the logged in user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        try
        {
            // check if the user owns the notification
            if (!await _notificationService.UserOwnsNotification(id, int.Parse(userIdString)))
            {
                throw new UnauthorizedAccessException();
            }

            await _notificationService.SetNotificationStatusByIdAsync(id, status);
            return Ok();
        }
        catch (FormatException)
        {
            return Unauthorized("Invalid User Id");
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("You dont have permission to update this notification");
        }
        catch (OverflowException)
        {
            return Unauthorized("Invalid User Id");
        }
        catch (Exception)
        {
            return Problem();
        }
    }
}