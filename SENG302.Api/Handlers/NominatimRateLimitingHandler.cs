using System.Threading.RateLimiting;

namespace SENG302.Api.Handlers;

public class NominatimRateLimitingHandler : DelegatingHandler
{
    private readonly TokenBucketRateLimiter _limiter ;

    public NominatimRateLimitingHandler(TokenBucketRateLimiter limiter)
    {
        _limiter = limiter;
    }
    
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