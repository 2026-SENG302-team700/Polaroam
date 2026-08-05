using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace SENG302.Api.Services;

public interface IImageTaggingService
{
    Task<IReadOnlyList<string>> GenerateTagsAsync(
        IFormFile image, CancellationToken cancellationToken = default
    );
}

public class ImageTaggingService : IImageTaggingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ImageTaggingService(
        HttpClient httpClient,
        IConfiguration configuration
    )
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    /// <summary>
    /// Sends a image to the LLM API and returns validated image tags
    /// 
    /// TODO: this method should eventually be called by a background queue worker.
    /// The controller would enquee new images (id) instead of waiting for the LLM request to complete.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<IReadOnlyList<string>> GenerateTagsAsync(
        IFormFile image, CancellationToken cancellationToken = default)
    {
        // checkl the image is valid
        ValidateImage(image);

        var apiKey = _configuration["LLM_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("The LLM API key has not been set");
        }

        await using var imageStream = image.OpenReadStream();
        using var memoryStream = new MemoryStream();

        await imageStream.CopyToAsync(memoryStream, cancellationToken);

        var encodedImage = Convert.ToBase64String(memoryStream.ToArray());
        var imageUrl = $"data:{image.ContentType};base64,{encodedImage}";

        var requestBody = new
        {
            model = "qwen-3.6-27b",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = """
                            Analyse this travel image and suggest between 3 and 5 tags.

                            Include only subjects, activities, scenery, weather and broad
                            location types that are clearly visible, only include place names if 100% obvious.

                            Return only valid JSON in the following format:
                            {"tags":["mountains","hiking","lake"]}
                            """
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = imageUrl
                            }
                        }
                    }
                }
            },
            stream = false,
            inject_tools = false,
            temperature = 0.2
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        // use the api key as a header
        request.Headers.TryAddWithoutValidation(
            "Authorization", apiKey
        );

        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException($"LLM request failed with status {(int)response.StatusCode}: {responseContent}");
        }

        using var responseDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken),
        cancellationToken: cancellationToken);

        var responseContentElement = responseDocument.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content");

        var content = responseContentElement.GetString();

        return ParseAndValidateTags(content);
    }

    /// <summary>
    /// Private method to interpret and parse the raw response from API.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static IReadOnlyList<string> ParseAndValidateTags(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "The LLM returned an empty response"
            );
        }

        TagResponse? tagResponse;

        try
        {
            // clean the response content
            var cleanedContent = content.Trim();
            if (cleanedContent.StartsWith("```"))
            {
                var firstLineEnd = cleanedContent.IndexOf('\n');
                if (firstLineEnd >= 0)
                {
                    cleanedContent = cleanedContent[(firstLineEnd + 1)..];
                }
                if (cleanedContent.EndsWith("```"))
                {
                    cleanedContent = cleanedContent[..^3];
                }
                cleanedContent = cleanedContent.Trim();
            }

            tagResponse = JsonSerializer.Deserialize<TagResponse>(
                cleanedContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("The LLM did not return valid JSON", exception);
        }

        // get the tags from the tag response and split into list
        var tags = tagResponse?.Tags?
        .Select(tag => tag.Trim().ToLowerInvariant())
        .Where(tag => tag.Length > 0 && tag.Length <= 50).Distinct().Take(5).ToList();

        if (tags == null || tags.Count == 0)
        {
            throw new InvalidOperationException("The LLM did not return any tags");
        }

        return tags;
    }

    /// <summary>
    /// Validate the image type provided, ensure it is one the API can process.
    /// </summary>
    /// <param name="image"></param>
    /// <exception cref="ArgumentException"></exception>
    private static void ValidateImage(IFormFile image)
    {
        if (image.Length == 0)
        {
            throw new ArgumentException("An image is required");
        }

        var supportedTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/jpg",
            "image/gif"
        };

        if (!supportedTypes.Contains(image.ContentType))
        {
            throw new ArgumentException("The uploaded file is not a supported image.");
        }
    }

    private record TagResponse(List<string> Tags);
}