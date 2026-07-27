namespace SENG302.Api.Models.Responses;

public class PhotoFile
{
    public required string FileName { get; set; }
    
    public required string ContentType { get; set; }
    
    public required string Base64String { get; set; }
}