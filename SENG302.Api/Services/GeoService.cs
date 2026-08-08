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
    
    /// <summary>
    /// Accepts a search query and calls the nominatim api to get a list
    /// of locations that match the query.
    /// </summary>
    /// <param name="query">Search Query</param>
    /// <param name="acceptLanguageHeader">the language for the user</param>
    /// <param name="cancellationToken">Allows request to be removed from queue when the user
    /// leaves the page</param>
    /// <returns>The list of locations</returns>
    /// <exception cref="HttpRequestException"></exception>
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
    
    /// <summary>
    /// Gets a location from an osm id
    /// </summary>
    /// <param name="osmId">The osm id of a location</param>
    /// <returns>Location information</returns>
    /// <exception cref="HttpRequestException"></exception>
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