using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Models.Responses;

public class NotificationResponse
{

    public NotificationResponse() { }

    [SetsRequiredMembers]
    public NotificationResponse(Notification baseNotification)
    {
        ReadStatus = baseNotification.ReadStatus;
        HeaderText = baseNotification.HeaderText;
        BodyText = baseNotification.BodyText;
        Timestamp = baseNotification.Timestamp;
        NotificationType = baseNotification.NotificationType;
        Handled = baseNotification.Handled;

        if (baseNotification is TripInvitationNotification)
        {
            TripId = ((TripInvitationNotification)baseNotification).TripId;
        }
    }

    public bool ReadStatus { get; set; }
    public bool Handled { get; set; }
    public required string HeaderText { get; set; }
    public required string BodyText { get; set; }
    public required DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public int? TripId { get; set; }
    public required string NotificationType { get; set; }
}
