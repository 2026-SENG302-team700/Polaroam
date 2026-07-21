using SENG302.Api.Models.Interfaces;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents updates to a pre-existing task.
/// TaskId needs to be present, everything else optional.
/// </summary>
public class UpdatePostRequest : IPostRequest
{
    public required int taskId { get; set; }
    
    public required string Title { get; set; }
    public string Location { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public required DateTime Date { get; set; }
    
}
