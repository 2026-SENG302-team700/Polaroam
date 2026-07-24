using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Responses;

public class GeoLocationSearchResult
{
    [Key]
    public required long OsmId { get; set; }
    public required string Lat { get; set; }
    public required string Lon { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
}