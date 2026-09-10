using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;


namespace SENG302.Api.Services;

public interface ICollaborationService
{
    List<CollaboratingUser> GetTripCollaborators(int tripId);
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

    async public List<CollaboratingUser> GetCollaboratingUsers(int tripId)
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
                    throw new KeyNotFoundException("The user is not found");
                }
                collaboratingUsers.Add(new CollaboratingUser(user));
            }
        }

        return collaboratingUsers;
    }
}