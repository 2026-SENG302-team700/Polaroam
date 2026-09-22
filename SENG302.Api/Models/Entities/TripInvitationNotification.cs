using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Models.Entities;

public class TripInvitationNotification : Notification
{
    public int TripId { get; set; }
}
