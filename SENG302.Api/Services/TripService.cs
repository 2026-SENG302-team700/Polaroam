using System.Globalization;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Resources.Helpers;
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

    /// <summary>
    /// Create a new trip object and do validation checks to see if the title and description meets valid length
    /// requirements and if start date is before end date.
    /// </summary>
    /// <param name="trip"></param>
    /// <param name="userId"></param>
    /// <returns>A newly created trip object</returns>
    /// <exception cref="ArgumentException">Thrown if the field is not valid</exception>
    public async Task<Trip> CreateNewTripAsync(NewTripRequest trip, int userId)
    {
        // Get a database context
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Clean request
        trip.Title = trip.Title.Trim();
        trip.Description = trip.Description.Trim();
        trip.StartDate = trip.StartDate == DateTime.MinValue ? null : trip.StartDate;
        trip.EndDate = trip.EndDate == DateTime.MinValue ? null : trip.EndDate;

        // Validation
        var errors = new Dictionary<string, List<string>>();

        foreach (var (field, errorList) in TripValidator.ValidateTitle(trip.Title))
        {
            if (errorList.Count > 0)
            {
                errors[field] = errorList;
            }
        }
        
        foreach (var (field, errorList) in TripValidator.ValidateDescription(trip.Description))
        {
            if (errorList.Count > 0)
            {
                errors[field] = errorList;
            }
        }

        if (errors.Any(e => e.Value.Count > 0))
        {
            throw new MultipleValidationListException(errors);
        }

        var user = await context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
        // Validate user email exists in db
        if (user == null)
        {
            throw new ArgumentException("User with the provided id does not exist.");
        }

        // Create the new trip
        var newTrip = new Trip()
        {
            Title = trip.Title,
            UserId = userId,
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

