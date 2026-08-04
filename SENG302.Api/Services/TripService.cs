using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Requests;
using SENG302.Api.Helpers.Validators;

namespace SENG302.Api.Services;

public interface ITripService
{
    Task<Trip> CreateNewTripAsync(NewTripRequest trip, int userId);
    Task<Trip> GetTripByIdAsync(int id);
    Task<IEnumerable<Trip>> GetTripsByUserIdAsync(int userId);
    Task DeleteTripByIdAsync(int tripId);
    Task<bool> UserHasModifyAccessToTrip(int userId, int tripId);
    Task<Trip> UpdateTripAsync(UpdateTripRequest tripRequest, int userId);
}

public class TripService : ITripService
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IUserService _userService;


    public TripService(IDbContextFactory<DatabaseContext> dbContextFactory, TimeProvider timeProvider, IUserService userService)
    {
        _dbContextFactory = dbContextFactory;
        _userService = userService;

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

        var user = await _userService.GetUserByIdAsync(userId);
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
        trip.StartDate = trip.StartDate.HasValue ? trip.StartDate : null;
        trip.EndDate = trip.EndDate.HasValue ? trip.EndDate : null;

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

        foreach (var (field, errorList) in TripValidator.ValidateDates(trip.StartDate, trip.EndDate))
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

        var user = await _userService.GetUserByIdAsync(userId);
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
    /// </summary>
    /// <param name="id">The id of the trip we are getting</param>
    /// <returns>The trip we have fetched</returns>
    /// <exception cref="ArgumentException">If the trip does not exist this exception is thrown</exception>
    public async Task<Trip> GetTripByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await context.Set<Trip>().Where(t => t.Id == id).FirstOrDefaultAsync();
        if (trip == null)
        {
            throw new ArgumentException("Trip with the provided ID does not exist.");
        }

        return trip;
    }

    /// <summary>
    /// Delete the trip from the database
    /// </summary>
    /// <param name="tripId">The id of the trip we are deleting</param>
    public async Task DeleteTripByIdAsync(int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await GetTripByIdAsync(tripId);

        context.Set<Trip>().Remove(trip);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Get if a user has access to a particular trip
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="tripId">The id of the trip</param>
    /// <returns>Whether the user has access to the trip specified</returns>
    public async Task<bool> UserHasModifyAccessToTrip(int userId, int tripId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await GetTripByIdAsync(tripId);

        return trip.UserId == userId;
    }

    /// <summary>
    /// Updates a trip with the given ID, Validates the title, description, and dates before updating the trip.
    /// Returns the updated trip if successful, or throws an ArgumentException if validation fails or the
    /// </summary>
    /// <param name="tripRequest"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Trip> UpdateTripAsync(UpdateTripRequest tripRequest, int userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trip = await context.Set<Trip>().Where(t => t.Id == tripRequest.Id).FirstOrDefaultAsync();
        if (trip == null)
        {
            throw new ArgumentException("trip with the provided ID does not exist.");
        }

        var startDate = tripRequest.StartDateSupplied ? tripRequest.StartDate : trip.StartDate;
        var endDate = tripRequest.EndDateSupplied ? tripRequest.EndDate : trip.EndDate;
        var title = tripRequest.Title?.Trim() ?? trip.Title.Trim();
        var description = tripRequest.Description?.Trim() ?? trip.Description.Trim();

        var errors = new Dictionary<string, List<string>>();

        foreach (var (field, errorList) in TripValidator.ValidateDates(startDate, endDate))
        {
            if (errorList.Count > 0)
            {
                errors[field] = errorList;
            }
        }

        foreach (var (field, errorList) in TripValidator.ValidateTitle(title))
        {
            if (errorList.Count > 0)
            {
                errors[field] = errorList;
            }
        }

        foreach (var (field, errorList) in TripValidator.ValidateDescription(description))
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

        // Update the trip
        if (!string.IsNullOrEmpty(tripRequest.Title?.Trim()))
            trip.Title = tripRequest.Title.Trim();

        if (!string.IsNullOrEmpty(tripRequest.Description?.Trim()))
            trip.Description = tripRequest.Description.Trim();

        if (tripRequest.StartDateSupplied)
            trip.StartDate = tripRequest.StartDate;

        if (tripRequest.EndDateSupplied)
            trip.EndDate = tripRequest.EndDate;

        await context.SaveChangesAsync();

        return trip;
    }
}