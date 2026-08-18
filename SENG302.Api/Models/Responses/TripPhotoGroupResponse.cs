namespace SENG302.Api.Models.Responses;

public class TripPhotoGroupResponse
{
    public int TripId { get; set; }
    public required string TripTitle { get; set; }
    public List<PhotoMetadataResponse> Photos { get; set; } = [];
}