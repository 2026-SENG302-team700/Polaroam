namespace SENG302.Api.Models.Requests;

public class ValidateOneTimeCodeRequest
{
    public required string Email { get; set; } = string.Empty;
    
    public required string Code { get; set; }
}