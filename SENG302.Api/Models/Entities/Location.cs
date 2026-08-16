using Microsoft.EntityFrameworkCore;

namespace SENG302.Api.Models.Responses;


[PrimaryKey(nameof(OsmId), nameof(PostId))]
public class Location
{
    public required long OsmId { get; set; }
    public string OsmType { get; set; } = "node";
    public required string Lat { get; set; }
    public required string Lon { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required int PostId { get; set; }
}
