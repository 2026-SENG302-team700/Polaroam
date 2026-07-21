using System.ComponentModel.DataAnnotations;
using SENG302.Api.Models.Interfaces;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents a task that a user can create.
/// TaskId is a variable set by the constructor when it's created, 
/// and should not be set after the case, so therefore it does not 
/// have a setter (and therefore, it cannot be set as required).
/// </summary>
public class NewPostRequest : IPostRequest
{
    public required int TripId { get; set; }
    
    public required string Title { get; set; }
    public required string Location { get; set; }
    public required string Content { get; set; }

    public DateTime Date { get; set; }
    
}
