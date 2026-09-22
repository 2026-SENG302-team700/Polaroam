namespace SENG302.Api.Models.Requests;

/**
 * DTO for accepting or declining a collaboration request.
 * Action is 0 for decline, 1 for accept.
 */
public class CollaborationRequest
{
    public required int Action {get; set;} // 0 = Decline, 1 = Accept
}