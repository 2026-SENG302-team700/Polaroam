using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Services;

public interface INotificationService
{
    Task AddNotificationAsync(Notification notification);
    Task<List<Notification>> GetNotificationsByUserIdAsync(int userId, int count, int offset = 0);
    Task SetNotificationStatusByIdAsync(int notificationId, bool status);
    Task SetNotificationHandledByIdAsync(int notificationId, bool handled);
    Task<bool> UserOwnsNotification(int notificationId, int userId);
    Task<int> GetUnreadCount(int userId);
    Task<bool> UserHasMoreThan(int userId, int limit, int offset = 0);
    Task SendInviteNotificationAsync(string title, int invitedUserId, int tripId);
}

public class NotificationService : INotificationService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public NotificationService(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Add a new notification to the database
    /// </summary>
    /// <param name="notification">the notification to add to database</param>
    public async Task AddNotificationAsync(Notification notification)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Returns a certain number of notificaitons for a specific user id,
    /// orders them by their read status and then by the timestamp
    /// </summary>
    /// <param name="userId">Id of the user to fetch notifications for</param>
    /// <param name="count">How many notifications to select from table and return</param>
    /// <param name="offset">notifications to skip when loading notifications</param>
    /// <returns>A list of notification responses ordered correctly</returns>
    public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId, int count, int offset = 0)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var notifications = await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.ReadStatus)
            .ThenByDescending(n => n.Timestamp)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
        
        return notifications;
    }

    /// <summary>
    /// Update a notiifcations status within the database
    /// </summary>
    /// <param name="notificationId">Id of the notification to update the status of</param>
    /// <param name="status">New status for the notification</param>
    public async Task SetNotificationStatusByIdAsync(int notificationId, bool status)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Notifications.Where(n => n.Id == notificationId).ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadStatus, status));
    }

    /// <summary>
    /// Update a notiifcations handled status within the database
    /// </summary>
    /// <param name="notificationId">Id of the notification to update the status of</param>
    /// <param name="handled">New handled status for the notification</param>
    public async Task SetNotificationHandledByIdAsync(int notificationId, bool handled)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Notifications.Where(n => n.Id == notificationId).ExecuteUpdateAsync(s => s.SetProperty(n => n.Handled, handled));
    }

    /// <summary>
    /// Check if a user owns a specific notification
    /// </summary>
    /// <param name="notificationId">The id of the notification to check owner of</param>
    /// <param name="userId">the user id to check if is owner</param>
    /// <returns></returns>
    public async Task<bool> UserOwnsNotification(int notificationId, int userId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Notifications.Where(n => n.Id == notificationId && n.UserId == userId).AnyAsync();
    }

    /// <summary>
    /// Get the number of notifications a user currently has
    /// </summary>
    /// <param name="userId">The user we are checking for notification count</param>
    /// <returns>The number of notifications a user currently has</returns>
    public async Task<int> GetUnreadCount(int userId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Notifications.Where(n => n.ReadStatus == false && n.UserId == userId).CountAsync();
    }

    /// <summary>
    /// Checks if a user has more notifications (in total) than count
    /// </summary>
    /// <param name="userId">User we are checking</param>
    /// <param name="limit">The number of rows retrieved </param>
    /// <param name="offset">offset for skipping notifications already classified</param>
    /// <returns>If the user has more than 'count' notifications</returns>
    public async Task<bool> UserHasMoreThan(int userId, int limit, int offset = 0)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        int count = await context.Notifications.Where(n => n.UserId == userId).CountAsync();
        return offset + limit < count;
    }

    /// <summary>
    /// creates an invitation notification for a trip invite and stores it with the given user.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="invitedUserId"></param>
    /// <returns></returns>
    public async Task SendInviteNotificationAsync(string title, int invitedUserId, int tripId)
    {
        var inviteNotification = new TripInvitationNotification
        {
            TripId = tripId,
            UserId = invitedUserId,
            HeaderText = "New Invitation Request",
            BodyText = $"You have been invited to collaborate on the {title} trip!",
            Timestamp = DateTimeOffset.UtcNow,
            NotificationType = "TripInvitation",
        };
        await AddNotificationAsync(inviteNotification);
    }
}