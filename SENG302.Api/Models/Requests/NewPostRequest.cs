using SENG302.Api.Models.Interfaces;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents a request by a user to create a post.
/// </summary>
public class NewPostRequest : IPostRequest
{
    public required int TripId { get; set; }

    public required string Title { get; set; }
    public required string Location { get; set; }
    public required string Content { get; set; }

    public DateTimeOffset? Date { get; set; }

}
