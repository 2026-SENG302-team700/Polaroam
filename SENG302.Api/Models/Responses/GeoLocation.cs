using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SENG302.Api.Models.Responses;

/// <summary>
/// Names are formatted in snake case to match json response from the nominatim api
/// </summary>
public class GeoLocationSearchResult
{
    [Key]
    [JsonPropertyName("osm_id")]
    public required long OsmId { get; set; }

    [JsonPropertyName("osm_type")]
    public string OsmType { get; set; } = "node";

    [JsonPropertyName("lat")]
    public required string Latitude { get; set; }

    [JsonPropertyName("lon")]
    public required string Longitude { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("display_name")]
    public required string DisplayName { get; set; }
}
