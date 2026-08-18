namespace SENG302.Api.Models.Responses;

public class ImageContextResponse
{
    public required int TripId { get; set; }
    public required string TripTitle { get; set; }
    public int? PostId { get; set; }
    public string? PostTitle { get; set; }
}