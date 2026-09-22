using System.Diagnostics.CodeAnalysis;
using SENG302.Api.Models.Entities;


namespace SENG302.Api.Models.Responses;

public class CollaboratingUserResponse
{

    public int UserId { get; set; }
    public required string DisplayName { get; set; }

    public CollaboratingUserResponse() {}

    [SetsRequiredMembers]
    public CollaboratingUserResponse(User user)
    {
        UserId = user.Id;
        DisplayName = user.DisplayName;
    }
}