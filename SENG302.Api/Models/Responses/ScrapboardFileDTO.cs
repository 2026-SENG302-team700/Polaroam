namespace SENG302.Api.Models.Responses;

public record ScrapboardFileDto(string Id, string MimeType, byte[] blob);