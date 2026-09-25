using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SENG302.Api.Models.Entities;

/// <summary> 
/// Object used to store if a user has been invited to trip and is yet to have signed up
/// </summary>
[Index(nameof(Email), IsUnique = true)]
public class PendingUser
{
    [Key]
    public required string Email { get; set; }
    public required List<int> TripInvites { get; set; } = [];
}