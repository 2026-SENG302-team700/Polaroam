using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Services;

public interface IScrapboardService
{
    public Task<Scrapboard> GetScrapboardByTripAsync(int tripId);
    public Task<ActionResult<Scrapboard>> CreateScrapboardAsync(int tripId, int UserId);
}

public class ScrapboardService : IScrapboardService
{
    private readonly ITripService _tripService;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public ScrapboardService(ITripService tripService, IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _tripService = tripService;
    }

    /// <summary>
    /// given a trip and user id, creates a scrapboard, and changes the trip's scrapboard id
    /// to be the id of the newly created one.
    /// If the trip's scrapboardid is greater than -1, then throw argument error as that trip
    /// must have a scrapboard attached to it.
    /// </summary>
    /// <param name="tripId"></param>
    /// <param name="UserId"></param>
    /// <returns>the newly created scrapboard</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<ActionResult<Scrapboard>> CreateScrapboardAsync(int tripId, int UserId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var trip = await _tripService.GetTripByIdAsync(tripId);
        //greater than -1 means the trip has a scrapboard already
        if (trip.HasScrapboard)
        {
            throw new ArgumentException("This trip already has a Scrapboard.");
        }
        var newScrapboard = new Scrapboard
        {
            OwnerId = [UserId],
            TripId = tripId,
            FileKey = "temporary-key", //<- **TEMPORARY. PLEASE ADD FILE FUNCTIONALITY**
        };
        trip.HasScrapboard = true;
        await _tripService.UpdateScrapboardReferenceAsync(tripId);
        var scrapboardEntry = context.Set<Scrapboard>().Add(newScrapboard);
        await context.SaveChangesAsync();

        return newScrapboard;
    }

    /// <summary>
    /// scrapboards use their tripid as their primary key, so
    /// given a trip's id, return it's corresponding scrapboard
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns>the scrapboard db item</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Scrapboard> GetScrapboardByTripAsync(int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var trip = await _tripService.GetTripByIdAsync(tripId);
        if (!trip.HasScrapboard)
        {
            throw new ArgumentException("This trip has no Scrapboard");
        }
        //if scrapboard id is greater than -1, then it must exist
        var scrapboard = await context.Scrapboards.FirstOrDefaultAsync(t => t.TripId == tripId);
        return scrapboard;

    }
}