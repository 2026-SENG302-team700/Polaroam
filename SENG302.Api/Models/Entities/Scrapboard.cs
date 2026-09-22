using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Keeps track of the metadata of the scrapboards for each trip
/// </summary>
public class Scrapboard
{ 
    [Key, ForeignKey("Trip")]
    public required int TripId {get;set;}

    public string Elements { get; set; } = "";

    public List<int> Files { get; set; } = [];
    
    public int Thumbnail { get; set; } = - 1;
}