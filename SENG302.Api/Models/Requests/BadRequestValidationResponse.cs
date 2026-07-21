namespace SENG302.Api.Models.Requests;

public class BadRequestValidationResponse
{
    public Dictionary<string, string> Errors { get; set; } = new();
}

public class BadRequestValidationListResponse
{
    public Dictionary<string, List<string>> Errors { get; set; } = new();
}