namespace SENG302.Api.Models.Responses;

public class UserResponse
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public required string Country { get; set; }

    public int ProfilePicture { get; set; }

    public float ProfilePictureOffsetX { get; set; }

    public float ProfilePictureOffsetY { get; set; }

    public float ProfilePictureZoom { get; set; } = 1;

    public bool AcceptedAiPrivacyPolicy { get; set; }

    public bool AiTagging { get; set; }
}