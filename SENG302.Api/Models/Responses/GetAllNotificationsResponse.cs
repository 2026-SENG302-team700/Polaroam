using SENG302.Api.Models.Responses;

namespace SENG302.Api.Models.Responses;

public class GetAllNotificationsResponse
{
    public required List<NotificationResponse> NotificationResponses { get; set; }
    public int UnreadCount { get; set; }
}