using System.Threading.RateLimiting;

public class NominatimRateLimitingHandler : DelegatingHandler
{
    private readonly TokenBucketRateLimiter _limiter = new(new TokenBucketRateLimiterOptions
    {
        TokenLimit = 1,                     // bucket holds at most 1 token = no bursting
        TokensPerPeriod = 1,                // refill 1 token...
        ReplenishmentPeriod = TimeSpan.FromSeconds(1), // ...every 1 second
        AutoReplenishment = true,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 15                     // queue everything rather than reject
    });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var lease = await _limiter.AcquireAsync(1, cancellationToken);

        if (!lease.IsAcquired)
        {
            // only happens if QueueLimit is hit and reached, or cancellation
            throw new HttpRequestException("Rate limit queue exceeded");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}