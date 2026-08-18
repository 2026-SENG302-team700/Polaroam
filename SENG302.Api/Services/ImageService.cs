using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Crmf;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Services;

public interface IImageService
{
    Task<List<string>> GetSuggestedTagsForUser(int userId, string query, int? tripId = null);
    Task<ImageContextResponse?> GetImageContextAsync(int photoId, int userId);
}

public class ImageService : IImageService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IFileService _fileService;

    public ImageService(IDbContextFactory<DatabaseContext> dbContextFactory, IFileService fileService)
    {
        _dbContextFactory = dbContextFactory;
        _fileService = fileService;
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

    /// <summary>
    /// Finds the post id from a provided photo id
    /// </summary>
    /// <param name="photoId">photo id</param>
    /// <param name="userId">user id, used for verifying file ownership</param>
    /// <returns>
    /// postId: the post id where the image has been found
    /// null: image not apart of any post (just part of a trip album)
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">if user does not own the file.</exception>
    public async Task<ImageContextResponse?> GetImageContextAsync(int photoId, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        if (!await _fileService.CheckFileOwnership(photoId, userId))
        {
            throw new UnauthorizedAccessException();
        }

        var album = await context.Albums.FirstOrDefaultAsync(a => a.PhotoIds.Contains(photoId));

        if (album == null)
        {
            throw new KeyNotFoundException("The image does not belong to any album.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == album.TripId && t.UserId == userId);

        if (trip == null)
        {
            throw new UnauthorizedAccessException();
        }

        var post = await context.Posts.FirstOrDefaultAsync(p => p.TripId == trip.Id && p.PhotoIds.Contains(photoId));

        return new ImageContextResponse
        {
            TripId = trip.Id,
            TripTitle = trip.Title,
            PostId = post?.PostId,
            PostTitle = post?.Title
        };
    }
}