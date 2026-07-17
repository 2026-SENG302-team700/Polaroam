using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents a list of tasks that a user can create. 
/// Each task list has a name and is associated with a user's email.
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
    public int UserId { get; set; } // Foreign key to the User that owns this task list

    public int Description { get; set; } = 0;
    [Required]
    public DateTime? StartDate { get; set; }
    [Required]
    public DateTime? EndDate { get; set; }
}
