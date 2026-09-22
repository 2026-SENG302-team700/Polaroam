using System.Text.Json.Serialization;

namespace SENG302.Api.Models.Requests;

public class InviteCollaboratorRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; set; }
}