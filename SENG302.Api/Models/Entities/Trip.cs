using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents a trip that a user can create. 
/// Each trip has a name and is associated with a user's id.
/// </summary>
public class Trip
{
    [Required]
    public int Id { get; set; }

    [MaxLength(128)]
    [MinLength(3)]
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public int UserId { get; set; } // Foreign key to the User that owns this trip

    public string Description { get; set; } = string.Empty;
    [Required]
    public DateOnly? StartDate { get; set; }
    [Required]
    public DateOnly? EndDate { get; set; }

    public List<int> AlbumPhotoIds { get; set; } = new ();
}
