using System.Diagnostics.CodeAnalysis;
using SENG302.Api.Models.Entities;


namespace SENG302.Api.Models.Responses;

public class CollaboratingUser
{

    public int UserId { get; set; }
    public required string DisplayName { get; set; }

    public CollaboratingUser() {}

    [SetsRequiredMembers]
    public CollaboratingUser(User user)
    {
        UserId = user.Id;
        DisplayName = user.DisplayName;
    }
}