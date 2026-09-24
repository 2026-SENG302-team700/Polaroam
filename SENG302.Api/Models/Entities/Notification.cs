using Microsoft.EntityFrameworkCore;

namespace SENG302.Api.Models.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool ReadStatus { get; set; } = false;
    public required string HeaderText { get; set; }
    public required string BodyText { get; set; }
    public required DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string NotificationType { get; set; } = null!;
    public bool Handled { get; set; } = false;
}
