using System.Net.Mime;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Services.ImageTagging;

public interface IImageTaggingService
{
    Task GenerateTagsAsync(
        IFormFile image, 
        int imageId, 
        CancellationToken cancellationToken = default
    );
}

public class ImageTaggingService : IImageTaggingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public ImageTaggingService(
        HttpClient httpClient,
        IConfiguration configuration,
        IDbContextFactory<DatabaseContext> dbContextFactory
    )
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Sends a image to the LLM API and returns validated image tags
    /// The controller would enqueue new images (id) instead of waiting for the LLM request to complete.
    /// </summary>
    /// <param name="image">An image file to be tagged</param>
    /// <param name="imageId">The id of the image to tag</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    public async Task GenerateTagsAsync(
        IFormFile image, int imageId, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateImage(image);
        }
        catch (ArgumentException e)
        {
            await SetTagErrors(imageId, "Tag cannot be generated");
            return;
        }
        
        
        var request = await GenerateRequest(image, cancellationToken);

        try
        {
            var content = await ProcessRequest(request, cancellationToken, imageId);
            await ParseValidateAndSetTags(content, imageId);
        }
        catch (HttpRequestException exception)
        {
            String errorMessage = exception.Message;
            await SetTagErrors(imageId, errorMessage);

        }
        catch (OperationCanceledException exception)
        {
            if (exception.InnerException is TimeoutException)
            {
                await SetTagErrors(imageId, "LLM request has timed out. Tags cannot be generated");
            }
            await SetTagErrors(imageId, exception.Message);
        }
    }
    
    /// <summary>
    /// Private method to generate the request to the LLM API.
    /// </summary>
    /// <param name="image">An image to generate tags for</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An HttpRequest that can be used to call the llm</returns>
    /// <exception cref="InvalidOperationException">Thrown if no api key provided</exception>
    private async Task<HttpRequestMessage> GenerateRequest(IFormFile image, CancellationToken cancellationToken)
    {
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

        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        // use the api key as a header
        request.Headers.TryAddWithoutValidation(
            "Authorization", apiKey
        );

        request.Content = JsonContent.Create(requestBody);
        return request;
    }

    /// <summary>
    /// Private method that sends the request to the LLM API and returns the response.
    /// </summary>
    /// <param name="request">The request method obtained from the GenerateRequest Method</param>
    /// <param name="cancellationToken"></param>
    /// <param name="imageId"></param>
    /// <returns>The json string returned from the request</returns>
    /// <exception cref="HttpRequestException">Thrown if received a non-success response code</exception>
    private async Task<string?> ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken, int imageId)
    {
        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken
        );
        
        if (!response.IsSuccessStatusCode)
        {
            
            await SetTagStatusOff(imageId);
            
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            throw new HttpRequestException($"LLM request failed with status {(int)response.StatusCode}: {responseContent}");
        }

        using var responseDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var responseContentElement = responseDocument.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content");

        String? content = responseContentElement.GetString();
        
        return content;
    }
    
    /// <summary>
    /// Private method to interpret and parse the raw response from API.
    /// </summary>
    /// <param name="content">Response returned from ProccessRequest</param>
    /// <param name="imageId">The image Id that was tagged</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task ParseValidateAndSetTags(string? content, int imageId)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            await SetTagStatusOff(imageId);

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
            
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var image = context.CustomFiles.FirstOrDefault(f => f.Id == imageId);
            
            if (image == null)
            {
                return;
            }
            
            if (tagResponse == null)
            {
                image.TagError = "Tagging Failure: No tags were found by the AI";
                image.CanRetryTagging = false;
                context.CustomFiles.Update(image);
                await context.SaveChangesAsync();
            }
            
            image.InTagQueue = false;
            image.Tags = tagResponse.Tags;
            image.CanRetryTagging = false;
            context.CustomFiles.Update(image);
            await context.SaveChangesAsync();
            
            Console.WriteLine("Tags set for image: " + imageId);
            
        }
        catch (JsonException exception)
        {
            await SetTagStatusOff(imageId);
            throw new InvalidOperationException("The LLM did not return valid JSON", exception);
        }

        // get the tags from the tag response and split into a list
        var tags = tagResponse?.Tags
        .Select(tag => tag.Trim().ToLowerInvariant())
        .Where(tag => tag.Length > 0 && tag.Length <= 50).Distinct().Take(5).ToList();

        if (tags == null || tags.Count == 0)
        {
            await SetTagStatusOff(imageId);
            throw new InvalidOperationException("The LLM did not return any tags");
        }
    }

    /// <summary>
    /// Validate the image type provided, ensure it is one that the API can process.
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

    /// <summary>
    /// Sets the tagging status to off.
    /// </summary>
    /// <param name="photoId"></param>
    private async Task SetTagStatusOff(int photoId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var image = context.CustomFiles.FirstOrDefault(f => f.Id == photoId);

        if (image == null)
        {
            return;
        }
        
        image.InTagQueue = false;
        context.CustomFiles.Update(image);
        await context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Sets the tagging status to off.
    /// </summary>
    /// <param name="photoId"></param>
    /// <param name="errorMessage"></param>
    private async Task SetTagErrors(int photoId, string errorMessage)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var image = context.CustomFiles.FirstOrDefault(f => f.Id == photoId);

        if (image == null)
        {
            return;
        }
        
        image.TagError = errorMessage;
        image.CanRetryTagging = false;
        context.CustomFiles.Update(image);
        await context.SaveChangesAsync();
    }

    private record TagResponse(List<string> Tags);
}