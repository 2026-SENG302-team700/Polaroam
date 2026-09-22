namespace SENG302.Api.Models.Responses;

public record ScrapboardDto(string Elements, List<ScrapboardFileDto> Files);