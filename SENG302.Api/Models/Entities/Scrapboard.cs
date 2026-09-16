using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Keeps track of the metadata of the scrapboards for each trip
/// </summary>
public class Scrapboard
{ 
    [ForeignKey("User")]
    public required int OwnerId { get; set; }

    [Key]
    public required int TripId {get;set;}

    public required string FileKey {get; set;}
}