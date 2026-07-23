using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Localization;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Services;

public interface IGeoService
{
    Task<List<GeoLocationSearchResult>> GetLocationsAsync(
        string query, 
        string? acceptLanguageHeader, 
        CancellationToken cancellationToken
    );
    Task<GeoLocationSearchResult?> GetLocationAsync(long osmId);
}

public class GeoService : IGeoService
{
    private readonly HttpClient _httpClient;

    public GeoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<List<GeoLocationSearchResult>> GetLocationsAsync(
        string query, 
        string? acceptLanguageHeader, 
        CancellationToken cancellationToken
        )
    {
        var q = new QueryBuilder
        {
            { "q", query },
            { "format","json" }
        };
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"search{q}");
        
        if (!string.IsNullOrEmpty(acceptLanguageHeader))
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguageHeader);
        }
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP error! status: {response.StatusCode}");
        }
        
        var results = await response.Content.ReadFromJsonAsync<List<GeoLocationSearchResult>>();

        return results;
    }

    public async Task<GeoLocationSearchResult?> GetLocationAsync(long osmId)
    {
        var q = new QueryBuilder
        {
            { "osm_ids", $"N{osmId},W{osmId},R{osmId}" },
            { "format", "json" }
        };

        var response = await _httpClient.GetAsync($"lookup{q}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP error! status: {response.StatusCode}");
        }

        var results = await response.Content.ReadFromJsonAsync<List<GeoLocationSearchResult>>();

        return results.FirstOrDefault();
    }
}