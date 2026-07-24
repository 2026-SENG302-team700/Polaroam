using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Responses;

public class GeoLocationSearchResult
{
    [Key]
    public required long osm_id { get; set; }
    public required string lat { get; set; }
    public required string lon { get; set; }
    public required string name { get; set; }
    public required string display_name { get; set; }
}