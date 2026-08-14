using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Services;

public interface IImageService
{
    public Task<List<string>> GetSuggestedTagsForUser(int userId, string query, int? tripId = null);
    public Task<List<int>> GetImagesFromQueryAsync(string query);
}

public class ImageService : IImageService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    
    public ImageService(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    /// <summary>
    /// Gets a list of suggested tags for a user based on the query.
    /// Will return a maximum of 5 tags.
    /// If a trip id is provided, only tags from that trip will be returned.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="query"></param>
    /// <param name="tripId"></param>
    /// <returns>List of tags to be suggested</returns>
    public async Task<List<string>> GetSuggestedTagsForUser(int userId, string query, int? tripId = null)
    {
        const int maxSuggestedTags = 5;

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Trip ids owned by this user
        var userTripIds = context.Trips
            .Where(t => t.UserId == userId)
            .Select(t => t.Id);

        // Start the query with albums that this user owns
        // Query creation assisted by claude code
        var albumsQuery = context.Albums
            .Where(a => userTripIds.Contains(a.TripId));
        
        // If a trip id is provided, filter the query to only include albums in that trip
        if (tripId.HasValue)
        {
            albumsQuery = albumsQuery.Where(a => a.TripId == tripId.Value);
        }

        // Get all photo ids from the albums
        var photoIdLists = await albumsQuery
            .Select(a => a.PhotoIds)
            .ToListAsync();
        
        var photoIds = photoIdLists
            .SelectMany(ids => ids)
            .Distinct()
            .ToList();

        if (photoIds.Count == 0)
        {
            return new List<string>();
        }

        // Pull Tags lists for matching files (Tags is nullable, so filter those out)
        var tagLists = await context.CustomFiles
            .Where(f => photoIds.Contains(f.Id) && f.Tags != null)
            .Select(f => f.Tags!)
            .ToListAsync();

        var tags = tagLists
            .SelectMany(t => t)
            .Where(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Take(maxSuggestedTags)
            .ToList();

        return tags;
    }

    public async Task<List<int>> GetImagesFromQueryAsync(string query)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();

        var photoIds = await context.CustomFiles.Where(img => img.Tags.Contains(query)).Select(img => img.Id).ToListAsync();

        return photoIds;
    }
}