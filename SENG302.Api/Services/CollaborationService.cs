using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;


namespace SENG302.Api.Services;

public interface ICollaborationService
{
    Task<List<CollaboratingUser>> GetTripCollaborators(int tripId);
    Task LeaveCollaboration(int userId, int tripId);
}

public class CollaborationService : ICollaborationService
{

    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IUserService _userService;
    private readonly ITripService _tripService;

    public CollaborationService(IDbContextFactory<DatabaseContext> dbContextFactory, IUserService userService, ITripService tripService)
    {
        _dbContextFactory = dbContextFactory;
        _userService = userService;
        _tripService = tripService;
    }

    /// <summary>
    /// Get all collaborators of a given trip and returns them
    /// </summary>
    /// <param name="tripId">id of the trip</param>
    /// <returns>All the collaborators of a trip</returns>
    /// <exception cref="KeyNotFoundException">If the trip id is invalid and does not correspond to the trip</exception>
    public async Task<List<CollaboratingUser>> GetTripCollaborators(int tripId)
    {
        var databaseContext = await _dbContextFactory.CreateDbContextAsync();

        var collaborators = await databaseContext.Trips.Where(t => t.Id == tripId).Select(t => t.Collaborators).FirstOrDefaultAsync();



        var collaboratingUsers = new List<CollaboratingUser>();

        if (collaborators != null)
        {
            foreach (int userId in collaborators)
            {
                User? user = await _userService.GetUserByIdAsync(userId);

                if (user is null)
                {
                    throw new KeyNotFoundException("The user could not be found");
                }
                collaboratingUsers.Add(new CollaboratingUser(user));
            }
        }
        else
        {
            throw new KeyNotFoundException("The trip could not be found");
        }

        return collaboratingUsers;
    }
    
    /// <summary>
    /// Takes a user and trio ID and attempt to remove the user as a collaborator
    /// </summary>
    /// <param name="userId">ID belonging to a user collaborating on the trip</param>
    /// <param name="tripId">The trip ID of which we wish to remove the user from </param>
    /// <exception cref="KeyNotFoundException">
    /// Throws exception if the trip could not be found or the user could not be removed from the trip</exception>
    public async Task LeaveCollaboration(int userId, int tripId)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
        if (trip is null) throw new KeyNotFoundException("The trip could not be found");
        
        if (!trip.Collaborators.Contains(userId))
            throw new KeyNotFoundException("The user could not be removed");
    
        trip.Collaborators = trip.Collaborators.Where(id => id != userId).ToList();
        await context.SaveChangesAsync();
    }
}