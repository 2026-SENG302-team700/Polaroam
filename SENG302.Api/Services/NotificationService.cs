using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Services;

public interface INotificationService
{
    Task AddNotificationAsync(Notification notification);
    Task<List<NotificationResponse>> GetNotificationsByUserIdAsync(int userId);
    Task SetNotificationStatusByIdAsync(int notificationId, bool status);
}

public class NotificationService : INotificationService
{

}