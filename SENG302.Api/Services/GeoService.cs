using Microsoft.AspNetCore.Http.Extensions;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Services;

public interface IGeoService
{
    Task<List<GeoLocationSearchResult>> GetLocationsAsync(string query);
}

public class GeoService : IGeoService
{
    public readonly HttpClient _httpClient;

    public GeoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<List<GeoLocationSearchResult>> GetLocationsAsync(string query)
    {
        var q = new QueryBuilder
        {
            { "q", query },
            { "format","json" }
        };

        var response = await _httpClient.GetAsync($"search{q}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP error! status: {response.StatusCode}");
        }
        
        var results = await response.Content.ReadFromJsonAsync<List<GeoLocationSearchResult>>();

        return results;
    }
}