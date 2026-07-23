using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents a Post that a user can create.
/// PostId is a variable set by the constructor when it's created, 
/// and should not be set after the case, so therefore it does not 
/// have a setter (and therefore, it cannot be set as required).
/// </summary>
public class Post
{
    [Key]
    public int PostId { get; init; }
    public required int TripId { get; set; }
    public required string Title { get; set; }
    public string Content { get; set; }
    public DateTime? Date { get; set; }
    public required string Location { get; set; }
    public DateTimeOffset creationTime { get; set; }
}