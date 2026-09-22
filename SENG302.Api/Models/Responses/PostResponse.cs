using System.Diagnostics.CodeAnalysis;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Models.Responses;

/// <summary>
/// The elements making up a post when sent to the front end
/// </summary>
public class PostResponse
{

    public PostResponse() { }

    [SetsRequiredMembers]
    public PostResponse(Post post, string displayName)
    {
        PostId = post.PostId;
        TripId = post.TripId;
        Title = post.Title;
        Content = post.Content;
        Date = post.Date;
        OsmId = post.OsmId;
        OsmType = post.OsmType;
        LocationName = post.LocationName;
        PhotoIds = post.PhotoIds;
        CreationTime = post.CreationTime;
        CreatorId = post.CreatorId;
        CreatorDisplayName = displayName;
    }

    public int PostId { get; init; }
    public required int TripId { get; set; }
    public required string Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset? Date
    {
        get
        {
            return field?.ToUniversalTime();
        }
        set
        {
            field = value?.ToUniversalTime();
        }
    }
    public required long OsmId { get; set; }
    public string OsmType { get; set; } = "node";
    public required string LocationName { get; set; }
    public List<int> PhotoIds { get; set; } = new();
    public DateTimeOffset CreationTime
    {
        get
        {
            return field.ToUniversalTime();
        }
        set
        {
            field = value.ToUniversalTime();
        }
    }
    public required int CreatorId { get; set; }
    public required string CreatorDisplayName { get; set; }
}
