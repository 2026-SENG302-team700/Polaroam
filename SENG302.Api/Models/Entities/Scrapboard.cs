using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SENG302.Api.Models.Entities;

public class Scrapboard
{
    [Key]
    public int Id { get; set; }
    
    [ForeignKey("User")]
    public required int OwnerId { get; set; }

    [ForeignKey("Trip")]
    public required int TripId {get;set;}
    public required string FileKey {get; set;}
}