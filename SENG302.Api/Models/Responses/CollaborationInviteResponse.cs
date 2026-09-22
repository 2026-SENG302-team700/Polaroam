using System.Text.Json.Serialization;

namespace SENG302.Api.Models.Responses;

public class CollaborationInviteResponse
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }
    
    [JsonPropertyName("message")]
    public required string Message { get; set; }
    
}