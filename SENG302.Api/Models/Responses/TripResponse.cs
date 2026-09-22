using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Responses;

/// <summary>
/// Represents a trip that a user can create. 
/// Each trip has a name and is associated with a user's id.
/// </summary>
public class TripResponse
{
    public required int Id { get; set; }

    [MaxLength(128)]
    [MinLength(3)]
    public required string Title { get; set; }

    public required int UserId { get; set; } // Foreign key to the User that owns this trip
    public required string OwnerName { get; set; }

    public required string Description { get; set; }

    public required DateTimeOffset? StartDate { get; set; }

    public required DateTimeOffset? EndDate { get; set; }

    public required List<CollaboratingUserResponse> Collaborators { get; set; } = [];
}
