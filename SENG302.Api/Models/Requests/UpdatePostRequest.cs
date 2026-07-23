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
    public long OsmId { get; set; }
    public string Content { get; set; } = string.Empty;
    public required DateTime Date { get; set; }

}
