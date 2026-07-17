using Microsoft.AspNetCore.Http.Extensions;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Services;

public interface IGeoService
{
    Task<GeoLocationSearchResult[]> GetLocations(string query);
}

public class GeoService : IGeoService
{
    public readonly HttpClient _httpClient;

    public GeoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task GetLocations(string query)
    {
        var q = new QueryBuilder
        {
            { "q", query },
            { "format","json" }
        };

        var response = await _httpClient.GetAsync($"search{q}");
        var body = await response.Content.ReadAsStringAsync();
        
        
    }
}