using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using SENG302.Api.Helpers.Validators;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.Email;
using SENG302.Api.Resources;


namespace SENG302.Api.Services;

public interface ICollaborationService
{
    Task<List<CollaboratingUserResponse>> GetTripCollaborators(int tripId);
    Task LeaveCollaboration(int userId, int tripId);
    Task SendInviteAsync(string userEmail, int inviterId, int tripId);
}

public class CollaborationService : ICollaborationService
{

    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IUserService _userService;
    private readonly ITripService _tripService;
    private readonly IEmailQueue _emailQueue;

    public CollaborationService(IDbContextFactory<DatabaseContext> dbContextFactory, IUserService userService, ITripService tripService, IEmailQueue emailQueue)
    {
        _dbContextFactory = dbContextFactory;
        _userService = userService;
        _tripService = tripService;
        _emailQueue = emailQueue;
    }

    /// <summary>
    /// Get all collaborators of a given trip and returns them
    /// </summary>
    /// <param name="tripId">id of the trip</param>
    /// <returns>All the collaborators of a trip</returns>
    /// <exception cref="KeyNotFoundException">If the trip id is invalid and does not correspond to the trip</exception>
    public async Task<List<CollaboratingUserResponse>> GetTripCollaborators(int tripId)
    {
        var databaseContext = await _dbContextFactory.CreateDbContextAsync();

        var collaborators = await databaseContext.Trips.Where(t => t.Id == tripId).Select(t => t.Collaborators).FirstOrDefaultAsync();



        var collaboratingUsers = new List<CollaboratingUserResponse>();

        if (collaborators != null)
        {
            foreach (int userId in collaborators)
            {
                User? user = await _userService.GetUserByIdAsync(userId);

                if (user is null)
                {
                    throw new KeyNotFoundException("The user could not be found");
                }
                collaboratingUsers.Add(new CollaboratingUserResponse(user));
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
    
    /// <summary>
    /// Send invite to the user
    /// </summary>
    /// <param name="userEmail">User Email</param>
    /// <param name="inviterId">id of the inviter</param>
    /// <param name="tripId">id of the trip</param>
    /// <exception cref="EmailInvalidationException">If email is invalid</exception>
    public async Task SendInviteAsync(string userEmail, int inviterId, int tripId)
    {
        
        if (!UserCredentialsValidator.CheckEmailFormat(userEmail))
        {
            throw new EmailInvalidationException("Email was invalid!");
        }
        
        var invitedUser = await _userService.GetUserFromEmailAsync(userEmail);
        var trip = await _tripService.GetTripByIdAsync(tripId);
        
        if (invitedUser is not null)
        {
            // user cannot send it to itself
            if (inviterId == invitedUser.Id)
            {
                throw new UserInTripException("You cannot invite yourself to a trip.");
            }
            
            // verify if user is already in trip
            var tripCollaborators = await GetTripCollaborators(tripId);
            if (
                tripCollaborators.Any(u => u.UserId == invitedUser.Id)
                || trip.UserId == invitedUser.Id
                )
            {
                throw new UserInTripException("This user is already apart of this trip.");
            }
        }
        
        // user cannot send invite to an already invited e-mail
        if (trip.PendingInvites.Contains(userEmail))
        {
            throw new PendingInviteException("This user has a pending invite to this trip.");
        }
        
        // email setup and logic
        var emailDict = new Dictionary<string, string>
        {
            { "TRIP_NAME", trip.Title },
            { "INVITER_NAME", await _userService.GetUserDisplayName(inviterId) },
            { "DISPLAY_NAME", invitedUser is not null? invitedUser.DisplayName : "there"}
        };
        
        await _emailQueue.QueueEmailAsync(new EmailJob(userEmail, EmailTemplate.CollaborationInvite, emailDict));
        
        // Update pending invites for trip
        var context = await _dbContextFactory.CreateDbContextAsync();
        trip.PendingInvites.Add(userEmail);
        await context.SaveChangesAsync();
    }
}