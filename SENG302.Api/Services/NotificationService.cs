using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Services;

public interface INotificationService
{
    Task AddNotificationAsync(Notification notification);
    Task<List<Notification>> GetNotificationsByUserIdAsync(int userId, int count);
    Task SetNotificationStatusByIdAsync(int notificationId, bool status);
    Task SetNotificationHandledByIdAsync(int notificationId, bool handled);
    Task<bool> UserOwnsNotification(int notificationId, int userId);
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
    /// <param name="notification">the noitification to add to database</param>
    public async Task AddNotificationAsync(Notification notification)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Returns a certain number of notificaitons for a specific user id,
    /// orders them by their read status and then by the timestamp
    /// </summary>
    /// <param name="userId">Id of the user to fetch notifications for</param>
    /// <param name="count">How many notifications to select from table and return</param>
    /// <returns>A list of notification responses ordered correctly</returns>
    public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId, int count)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var notifications = await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.ReadStatus)
            .ThenBy(n => n.Timestamp)
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
}