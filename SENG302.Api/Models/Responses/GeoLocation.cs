namespace SENG302.Api.Models.Responses;

public class GeoLocationSearchResult
{
    public int OsmId { get; set; };
    public string Latitude { get; set; }
    public string Longitude { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
}