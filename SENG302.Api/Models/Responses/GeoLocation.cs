namespace SENG302.Api.Models.Responses;

public class GeoLocationSearchResult
{
    public long osm_id { get; set; }
    public string lat { get; set; }
    public string lon { get; set; }
    public string name { get; set; }
    public string display_name { get; set; }
}