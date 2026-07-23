using SENG302.Api.Models.Interfaces;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents updates to a pre-existing post.
/// PostId needs to be present, everything else optional.
/// </summary>
public class UpdatePostRequest : IPostRequest
{
    public required int Id { get; set; }
    public required int TripId { get; set; }
    public required string Title { get; set; }
    public string Location { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public required DateTimeOffset? Date { get; set; }
    public bool NoContentAllowed { get; set; } = false;

}
