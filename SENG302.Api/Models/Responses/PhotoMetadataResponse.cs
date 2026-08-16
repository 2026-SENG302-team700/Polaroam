namespace SENG302.Api.Models.Responses;

public class PhotoMetadataResponse
{
    public int PhotoId { get; set; }
    public required string FileName { get; set; }
    public required string MimeType { get; set; }
    public required List<string> Tags { get; set; } = new();
    public required DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

}