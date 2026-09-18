using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public bool HasScrapboard { get; set; } = false;

    [Required]
    public int UserId { get; set; } // Foreign key to the User that owns this trip

    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset? StartDate
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

    [Required]
    public DateTimeOffset? EndDate
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

    public List<int> Collaborators { get; set; } = [];
}
