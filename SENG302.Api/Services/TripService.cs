using System.Globalization;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Resources.Helpers;
using System.Reflection;
using SENG302.Api.Models.Requests;

namespace SENG302.Api.Services;

public interface ITripService
{
    Task<Trip> CreateNewTripAsync(NewTripRequest trip, int userId);
    Task<Trip> GetTripByIdAsync(int id, int userId);
    Task<IEnumerable<Trip>> GetTripsByUserIdAsync(int userId);
}

public class TripService : ITripService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;


    public TripService(IDbContextFactory<DatabaseContext> dbContextFactory, TimeProvider timeProvider)
    {
        _dbContextFactory = dbContextFactory;

    }

    /// <summary>
    /// Gets all trips associated with a user's email and checks for profanity if applicable
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Trip>> GetTripsByUserIdAsync(int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trips = await context.Set<Trip>().Where(t => t.UserId == userId).ToListAsync();

        var user = await context.Users.FindAsync(userId);
        // check for profanity filtering
        if (user?.ProfanityFiltering != true) return trips;

        var profanityFiltering = new ProfanityFilter.ProfanityFilter();
        foreach (Trip trip in trips)
        {
            trip.Title = profanityFiltering.CensorString(trip.Title);
        }
        return trips;
    }


    public async Task<Trip> CreateNewTripAsync(NewTripRequest trip, int userId)
    {
        // Get a database context
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Clean request
        trip.Title = trip.Title.Trim();
        trip.Description = trip.Description.Trim();
        trip.StartDate = trip.StartDate == DateTime.MinValue ? null : trip.StartDate;
        trip.EndDate = trip.EndDate == DateTime.MinValue ? null : trip.EndDate;

        // Validate name length
        if (string.IsNullOrEmpty(trip.Title) || trip.Title.Length < 3 || trip.Title.Length > 128)
        {
            throw new ArgumentException("Trip name is required and must be between 3 and 128 characters long");
        }
        // Validate title characters (only allow letters, numbers, spaces, hyphens, and apostrophes)
        if (!ValidationPatterns.TripTitle.IsMatch(trip.Title))
        {
            throw new ArgumentException("Trip name cannot contain characters other than letters, spaces, hyphens, apostrophes, or numbers");
        }

        if (trip.Description.Length > 2048)
        {
            throw new ArgumentException("Description must be 2048 characters or less");
        }

        // validation start and end date
        // check end date is after start date
        if (trip.EndDate < trip.StartDate)
        {
            throw new ArgumentException("Trip start date must be before trip end date");
        }
        
        var user = await context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
        // Validate user email exists in db
        if (user == null)
        {
            throw new ArgumentException("User with the provided id does not exist.");
        }

        // check for profanity in the name
        if (ProfanityTools.ContainsProfanity(trip.Title, user.ProfanityFiltering))
        {
            throw new ArgumentException("Trip name cannot contain profanity.");
        }

        // Create the new trip
        var newTrip = new Trip()
        {
            Title = trip.Title,
            Description = trip.Description,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate
        };
        context.Set<Trip>().Add(newTrip);
        await context.SaveChangesAsync();
        return newTrip;
    }

    /// <summary>
    /// Gets a trip by its ID. Returns null if no trip with the given ID exists.
    /// </summary>trip
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Trip> GetTripByIdAsync(int id, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await context.Set<Trip>().Where(t => t.Id == id).FirstOrDefaultAsync();
        if (trip == null)
        {
            return null;
        }
        var user = await context.Users.FindAsync(userId);
        // check for profanity censor
        if (user?.ProfanityFiltering != true) return trip;

        var profanityFilter = new ProfanityFilter.ProfanityFilter();
        trip.Title = profanityFilter.CensorString(trip.Title);
        return trip;
    }

    /// <summary>
    /// Gets a trip by its ID. Returns null if no trip with the given ID exists.
    /// Also takes a DatabaseContext so the list that it returns can be modified.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Trip> GetTripByIdAsync(int id, DatabaseContext context)
    {
        var trip = await context.Set<Trip>().Where(t => t.Id == id).FirstOrDefaultAsync();
        if (trip == null)
        {
            throw new ArgumentException("trip with the provided ID does not exist.");
        }

        return trip;
    }
}

