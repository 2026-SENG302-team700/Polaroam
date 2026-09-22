using Microsoft.EntityFrameworkCore;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using SENG302.Api.Helpers.Validators;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.Email;


namespace SENG302.Api.Services;

public interface ICollaborationService
{
    Task<List<CollaboratingUserResponse>> GetTripCollaborators(int tripId);
    Task LeaveCollaboration(int userId, int tripId);
    Task SendInviteAsync(string userEmail, int inviterId, int tripId);
    Task ActionRejectAsync(int tripId, User user);
    Task ActionAcceptAsync(int tripId, User user);
}


public class CollaborationService : ICollaborationService
{

    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IUserService _userService;
    private readonly ITripService _tripService;
    private readonly IEmailQueue _emailQueue;
    private readonly INotificationService _notificationService;

    public CollaborationService(IDbContextFactory<DatabaseContext> dbContextFactory, IUserService userService, ITripService tripService, IEmailQueue emailQueue, INotificationService notificationService
    )
    {
        _dbContextFactory = dbContextFactory;
        _userService = userService;
        _tripService = tripService;
        _emailQueue = emailQueue;
        _notificationService = notificationService;
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
            throw new EmailInvalidationException("Invalid email address. Email must be in the format 'jane@doe.nz'");
        }

        var invitedUser = await _userService.GetUserFromEmailAsync(userEmail);
        var trip = await _tripService.GetTripByIdAsync(tripId);

        if (invitedUser is not null)
        {
            // user cannot send it to itself
            if (inviterId == invitedUser.Id)
            {
                throw new UserInTripException("You can not invite yourself to a trip.");
            }

            // verify if user is already in trip
            var tripCollaborators = await GetTripCollaborators(tripId);
            if (
                tripCollaborators.Any(u => u.UserId == invitedUser.Id)
                || trip.UserId == invitedUser.Id
                )
            {
                throw new UserInTripException("This user is already a part of this trip.");
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
        //create a pending user so that references can be updated accordingly when they create an account
        if (invitedUser == null)
        {
            await _userService.CreateUpdatePendingUserAsync(userEmail, trip.Id);
        }
        else
        {
            await _notificationService.SendInviteNotificationAsync(trip.Title, invitedUser.Id, trip.Id);
        }

        await context.SaveChangesAsync();

    }




    /// <summary>
    /// If the given user is labelled as a pending collaborator on the given task, then 
    /// this method will remove them from the pending column and will add them to the collaborators column.
    /// </summary>
    /// <param name="tripId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task ActionAcceptAsync(int tripId, User user)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var trip = await _tripService.GetTripByIdAsync(tripId);
        if (trip.PendingInvites.Remove(user.Email))
        {
            trip.Collaborators.Add(user.Id);
            await context.SaveChangesAsync();
            var acceptNotificationForOwner = new Notification()
            {
                UserId = trip.UserId, // send to owner
                HeaderText = "Collaboration accepted!",
                BodyText = $"{user.DisplayName} has accepted your collaboration request for your {trip.Title} trip!",
                Timestamp = DateTimeOffset.Now,
            };
            await _notificationService.AddNotificationAsync(acceptNotificationForOwner);
        }
        else
        {
            throw new ArgumentException("User has no pending invite on this task");
        }
    }

    /// <summary>
    /// If the given user is labelled as a pending collaborator on the given task, then 
    /// this method will remove them from the pending column.
    /// </summary>
    /// <param name="tripId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task ActionRejectAsync(int tripId, User user)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var trip = await _tripService.GetTripByIdAsync(tripId);
        if (trip.PendingInvites.Remove(user.Email))
        {
            await context.SaveChangesAsync();
            var rejectNotificationForOwner = new Notification()
            {
                UserId = trip.UserId, // send to owner
                HeaderText = "Collaboration rejected.",
                BodyText = $"{user.DisplayName} has rejected your collaboration request for your {trip.Title} trip.",
                Timestamp = DateTimeOffset.Now,
            };
            await _notificationService.AddNotificationAsync(rejectNotificationForOwner);
        }
        else
        {
            throw new ArgumentException("User has no pending invite on this task");
        }
    }
}