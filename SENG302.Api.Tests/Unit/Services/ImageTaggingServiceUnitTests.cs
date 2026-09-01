using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using SENG302.Api.Services.ImageTagging;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Services;

public class ImageTaggingServiceUnitTests : DatabaseUnitTestFixture
{
    private sealed class TestHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory
    ) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestContent { get; private set; }
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            LastRequest = request;
            LastRequestContent = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            RequestCount++;
            return await responseFactory(request, cancellationToken);
        }
    }

    private readonly IImageTaggingQueue _imageTaggingQueue =
        Substitute.For<IImageTaggingQueue>();
    private readonly IFileService _fileService = Substitute.For<IFileService>();

    private static IFormFile CreateImage(
        string contentType = "image/png",
        byte[]? content = null
    )
    {
        content ??= [0x89, 0x50, 0x4E, 0x47];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "image", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static HttpResponseMessage CreateLlmResponse(string? content)
    {
        var responseBody = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new { content }
                }
            }
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                responseBody,
                Encoding.UTF8,
                "application/json"
            )
        };
    }

    private static TestHttpMessageHandler CreateHandler(
        HttpResponseMessage response
    )
    {
        return new TestHttpMessageHandler((_, _) => Task.FromResult(response));
    }

    private ImageTaggingService CreateService(
        TestHttpMessageHandler handler,
        string? apiKey = "test-api-key"
    )
    {
        var configurationValues = new Dictionary<string, string?>
        {
            ["LLM_API_KEY"] = apiKey
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://llm.test/")
        };

        return new ImageTaggingService(
            client,
            configuration,
            _mockDbContextFactory,
            _imageTaggingQueue,
            _fileService
        );
    }

    private async Task<int> CreateStoredImage()
    {
        await using var context =
            await _mockDbContextFactory.CreateDbContextAsync();
        var image = new CustomFile
        {
            OwnerId = 1,
            FileKey = "test.png",
            OriginalFileName = "test.png",
            MimeType = "image/png",
            FileSize = 4,
            InTagQueue = true,
            CanRetryTagging = true
        };
        context.CustomFiles.Add(image);
        await context.SaveChangesAsync();
        return image.Id;
    }

    private async Task<CustomFile> GetStoredImage(int imageId)
    {
        await using var context =
            await _mockDbContextFactory.CreateDbContextAsync();
        return context.CustomFiles.Single(image => image.Id == imageId);
    }

    [Fact]
    public async Task GenerateTagsAsync_ValidResponse_SavesTags()
    {
        var tagsContent = JsonSerializer.Serialize(new
        {
            tags = new[] { "beach", "ocean", "sunset" }
        });
        var handler = CreateHandler(CreateLlmResponse(tagsContent));
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        await service.GenerateTagsAsync(CreateImage(), imageId);

        var storedImage = await GetStoredImage(imageId);
        storedImage.Tags.ShouldBe(["beach", "ocean", "sunset"]);
        storedImage.InTagQueue.ShouldBeFalse();
        storedImage.CanRetryTagging.ShouldBeFalse();
        storedImage.TagError.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateTagsAsync_ValidResponse_SendsCorrectRequest()
    {
        var handler = CreateHandler(
            CreateLlmResponse("{\"tags\":[\"beach\"]}")
        );
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        await service.GenerateTagsAsync(CreateImage(), imageId);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.RequestUri.ShouldNotBeNull();
        handler.LastRequest.RequestUri.AbsolutePath.ShouldBe("/chat/completions");
        handler.LastRequest.Headers.GetValues("Authorization").Single()
            .ShouldBe("test-api-key");
        handler.LastRequestContent.ShouldNotBeNull();
        handler.LastRequestContent.ShouldContain("qwen-3.6-27b");
        handler.LastRequestContent.ShouldContain("data:image/png;base64,");
    }

    [Fact]
    public async Task GenerateTagsAsync_MarkdownJsonResponse_SavesTags()
    {
        var handler = CreateHandler(
            CreateLlmResponse(
                "```json\n{\"tags\":[\"mountains\",\"hiking\"]}\n```"
            )
        );
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        await service.GenerateTagsAsync(CreateImage(), imageId);

        var storedImage = await GetStoredImage(imageId);
        storedImage.Tags.ShouldBe(["mountains", "hiking"]);
        storedImage.InTagQueue.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateTagsAsync_UnsupportedImage_SetsTagErrorAndDoesNotSendRequest()
    {
        var handler = CreateHandler(
            CreateLlmResponse("{\"tags\":[\"document\"]}")
        );
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        await service.GenerateTagsAsync(
            CreateImage("application/pdf"),
            imageId
        );

        var storedImage = await GetStoredImage(imageId);
        storedImage.TagError.ShouldBe("Tag cannot be generated");
        storedImage.CanRetryTagging.ShouldBeFalse();
        handler.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateTagsAsync_EmptyImage_SetsTagErrorAndDoesNotSendRequest()
    {
        var handler = CreateHandler(
            CreateLlmResponse("{\"tags\":[\"image\"]}")
        );
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        await service.GenerateTagsAsync(
            CreateImage(content: []),
            imageId
        );

        var storedImage = await GetStoredImage(imageId);
        storedImage.TagError.ShouldBe("Tag cannot be generated");
        storedImage.CanRetryTagging.ShouldBeFalse();
        handler.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateTagsAsync_ApiKeyNotSet_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler(
            CreateLlmResponse("{\"tags\":[\"beach\"]}")
        );
        var service = CreateService(handler, apiKey: null);
        var imageId = await CreateStoredImage();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.GenerateTagsAsync(CreateImage(), imageId)
        );

        exception.Message.ShouldBe("The LLM API key has not been set");
        handler.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateTagsAsync_UnsuccessfulResponse_SetsTagError()
    {
        var handler = CreateHandler(
            new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("LLM unavailable")
            }
        );
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        await service.GenerateTagsAsync(CreateImage(), imageId);

        var storedImage = await GetStoredImage(imageId);
        storedImage.InTagQueue.ShouldBeFalse();
        storedImage.CanRetryTagging.ShouldBeFalse();
        storedImage.TagError.ShouldNotBeNull();
        storedImage.TagError.ShouldContain("502");
        storedImage.TagError.ShouldContain("LLM unavailable");
    }

    [Fact]
    public async Task GenerateTagsAsync_EmptyResponse_ThrowsInvalidOperationExceptionAndStopsTagging()
    {
        var handler = CreateHandler(CreateLlmResponse(""));
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.GenerateTagsAsync(CreateImage(), imageId)
        );

        exception.Message.ShouldBe("The LLM returned an empty response");
        var storedImage = await GetStoredImage(imageId);
        storedImage.InTagQueue.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateTagsAsync_InvalidJson_ThrowsInvalidOperationExceptionAndStopsTagging()
    {
        var handler = CreateHandler(CreateLlmResponse("not json"));
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.GenerateTagsAsync(CreateImage(), imageId)
        );

        exception.Message.ShouldBe("The LLM did not return valid JSON");
        var storedImage = await GetStoredImage(imageId);
        storedImage.InTagQueue.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateTagsAsync_NoTags_ThrowsInvalidOperationExceptionAndStopsTagging()
    {
        var handler = CreateHandler(CreateLlmResponse("{\"tags\":[]}"));
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.GenerateTagsAsync(CreateImage(), imageId)
        );

        exception.Message.ShouldBe("The LLM did not return any tags");
        var storedImage = await GetStoredImage(imageId);
        storedImage.InTagQueue.ShouldBeFalse();
        storedImage.Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task GenerateTagsAsync_CancelledRequest_SetsTagError()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(
                new OperationCanceledException("Request was cancelled")
            )
        );
        var service = CreateService(handler);
        var imageId = await CreateStoredImage();

        await service.GenerateTagsAsync(CreateImage(), imageId);

        var storedImage = await GetStoredImage(imageId);
        storedImage.TagError.ShouldBe("Request was cancelled");
        storedImage.CanRetryTagging.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateTagsAsync_ImageDoesNotExist_DoesNotThrowException()
    {
        var handler = CreateHandler(
            CreateLlmResponse("{\"tags\":[\"beach\"]}")
        );
        var service = CreateService(handler);

        await Should.NotThrowAsync(() =>
            service.GenerateTagsAsync(CreateImage(), 999)
        );
    }
}
