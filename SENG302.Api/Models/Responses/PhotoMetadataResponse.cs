public class PhotoMetadataResponse
{
    public int PhotoId { get; set; }
    public required string FileName { get; set; }
    public required string MimeType { get; set; }
    public int TripId { get; set; }
}