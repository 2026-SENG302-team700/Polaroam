using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SENG302.Api.Models.Entities;

public class Scrapboard
{ 
    [ForeignKey("User")]
    public required int[] OwnerId { get; set; }

    [Key]
    [ForeignKey("Trip")]
    public required int TripId {get;set;}

    public required string FileKey {get; set;}
}