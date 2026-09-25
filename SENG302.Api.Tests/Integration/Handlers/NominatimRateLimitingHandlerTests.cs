using System.Net;
using System.Threading.RateLimiting;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Handlers;

public class NominatimRateLimitingHandlerTests
{
    private sealed class RecordingHttpHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
            );
        }
    }

    [Fact]
    public async Task SendAsync_TokenAvailable_ForwardsRequest()
    {
        using var limiter = CreateLimiter(queueLimit: 0);
        var innerHandler = new RecordingHttpHandler();

        using var handler = new NominatimRateLimitingHandler(limiter)
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpMessageInvoker(handler);

        var response = await client.SendAsync(
            new HttpRequestMessage(
                HttpMethod.Get,
                "https://nominatim.test/search"
            ),
            CancellationToken.None
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task SendAsync_QueueFull_ThrowsAndDoesNotForwardTheRequest()
    {
        using var limiter = CreateLimiter(queueLimit: 0);

        // use only the avaliable tokens
        using var existingLease = limiter.AttemptAcquire(1);
        existingLease.IsAcquired.ShouldBeTrue();

        var innerHandler = new RecordingHttpHandler();

        using var handler = new NominatimRateLimitingHandler(limiter)
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpMessageInvoker(handler);

        var exception = await Should.ThrowAsync<HttpRequestException>(
            () => client.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://nominatim.test/search"
                ),
                CancellationToken.None
            )
        );

        exception.Message.ShouldBe("Rate limit queue exceeded");
        innerHandler.RequestCount.ShouldBe(0);
    }

    /// <summary>
    /// Create a test rate limiter to use within tests
    /// </summary>
    /// <param name="queueLimit"></param>
    /// <returns></returns>
    private static TokenBucketRateLimiter CreateLimiter(int queueLimit)
    {
        return new TokenBucketRateLimiter(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromHours(1),
                AutoReplenishment = false,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = queueLimit
            }
        );
    }
}