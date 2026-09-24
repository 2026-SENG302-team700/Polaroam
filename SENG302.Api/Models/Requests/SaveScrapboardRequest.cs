namespace SENG302.Api.Models.Requests;

public class SaveScrapboardRequest
{
    public int TripId { get; set; }
    public string Elements { get; set;}
    public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    public IFormFile Thumbnail { get; set; }
}