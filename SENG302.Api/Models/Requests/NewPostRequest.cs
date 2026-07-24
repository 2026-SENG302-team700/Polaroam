using System.ComponentModel.DataAnnotations;
using SENG302.Api.Models.Interfaces;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents a request by a user to create a post.
/// </summary>
public class NewPostRequest : IPostRequest
{
    public required int TripId { get; set; }

    public required string Title { get; set; }
    public required long OsmId { get; set; }
    public required string Content { get; set; }

    public DateTime Date { get; set; }

}
