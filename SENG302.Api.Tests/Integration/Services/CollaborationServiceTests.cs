using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Reqnroll.Assist;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services;
using SENG302.Api.Services.Email;
using Shouldly;
using Xunit.Abstractions;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.Services;

public class CollaborationServiceTests : BaseIntegrationTestFixture
{
    private ICollaborationService ServiceUnderTest => ServiceProvider.GetRequiredService<ICollaborationService>();
    private readonly IUserService _mockUserService;
    private ITestOutputHelper _output;
    private IEmailQueue _mockEmailQueue;

    public CollaborationServiceTests(WebApplicationFactory<Program> webAppFactory, ITestOutputHelper output) : base(webAppFactory)
    {
        _mockUserService = Substitute.For<IUserService>();
        _output = output;
        _mockEmailQueue = Substitute.For<IEmailQueue>();
    }
    
    
    /// <summary>
    /// Creates and adds to the database a test user for these tests
    /// </summary>
    private async Task AddTestUser()
    {

        User testUser = new()
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        };
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Users.Add(testUser);
        await context.SaveChangesAsync();

        _mockUserService.GetUserByIdAsync(testUser.Id).Returns(Task.FromResult((User?)testUser));
    }

    [Fact]
    public async Task GetTripCollaborators_NoCollaborators_ReturnEmptyList()
    {
        await AddTestUser();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21)
        };

        await using var context = await DbContextFactory.CreateDbContextAsync();

        context.Trips.Add(trip);

        await context.SaveChangesAsync();

        var collaborators = await ServiceUnderTest.GetTripCollaborators(1);
        
        collaborators.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task GetTripCollaborators_TwoCollaborators_ReturnListWithTwoCollaboratorIds()
    {
        await AddTestUser();
        
        await using var context = await DbContextFactory.CreateDbContextAsync();

        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        User user3 = new()
        {
            Id = 3,
            Email = "test3@example.com",
            DisplayName = "Test User 3",
            PasswordKey = "password",
            Country = "Test Country"
        };
        
        context.Users.Add(user2);
        context.Users.Add(user3);

        await context.SaveChangesAsync();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2, 3]
        };

        context.Trips.Add(trip);
        
        await context.SaveChangesAsync(); 

        var collaborators = await ServiceUnderTest.GetTripCollaborators(1);
        
        collaborators.Count.ShouldBe(2);
    }
    
    [Fact]
    public async Task GetTripCollaborators_TripDoesNotExist_ReturnsKeyNotFoundException()
    {
        await AddTestUser();
        
        await using var context = await DbContextFactory.CreateDbContextAsync();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2, 3]
        };

        context.Trips.Add(trip);
        
        await context.SaveChangesAsync(); 
        

        await Should.ThrowAsync<KeyNotFoundException>(
            async () => await ServiceUnderTest.GetTripCollaborators(4)
        );
    }
    
    [Fact]
    public async Task GetTripCollaborators_OwnerDoesNotExist_ReturnsKeyNotFoundException()
    {
        await AddTestUser();
        
        await using var context = await DbContextFactory.CreateDbContextAsync();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 3,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2, 3]
        };

        context.Trips.Add(trip);
        
        await context.SaveChangesAsync(); 
        

        await Should.ThrowAsync<KeyNotFoundException>(
            async () => await ServiceUnderTest.GetTripCollaborators(1)
        );
    }

    [Fact]
    public async Task LeaveCollaboration_ValidRequest_RemovesUser()
    {
        // Setup
        var trip = new Trip
        {
            Id = 1,
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2, 3]
        };
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Add(trip);
        await context.SaveChangesAsync();

        // Leave
        await ServiceUnderTest.LeaveCollaboration(2, 1);

        // Check
        await context.Entry(trip).ReloadAsync();
        trip.Collaborators.Count.ShouldBe(1);
        trip.Collaborators.ShouldNotContain(2);
    }

    [Fact]
    public async Task LeaveCollaboration_NotCollaborator_ThrowsKeyNotFoundException()
    {
        // Setup
        var trip = new Trip
        {
            Id = 1,
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [3]
        };
        await using var context = await DbContextFactory.CreateDbContextAsync();
        context.Add(trip);
        await context.SaveChangesAsync();

        // Check
        await ServiceUnderTest.LeaveCollaboration(2, 1).ShouldThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task LeaveCollaboration_NoTrip_ThrowsKeyNotFoundException()
    {
        await ServiceUnderTest.LeaveCollaboration(2, 1).ShouldThrowAsync<KeyNotFoundException>();
    }
    [Theory]
    [InlineData("vlad.nistor.email")]
    [InlineData("pandya@gmail+!.co#m")]
    [InlineData(".@gmail.com")]
    [InlineData("cool..man@gmail.com")]
    [InlineData("person@yahoo.")]
    [InlineData("crazy.@gmail.nz")]
    [InlineData("CrazyyBoyyCrazyyBoyyCrazyyBoyyCrazyyBoyyCrazyyBoyyCrazyyBoyyCrazyyBoyy@gmail.com")]
    [InlineData("man@gmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompanygmailiscom.comcompany")]
    [InlineData("froggy@-outlook.com")]
    [InlineData("froggy@outlook.com-")]
    [InlineData("crazy@.nz")]
    [InlineData("abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghij@abcdefghijabcdefghijabcdefgh")]
    [InlineData("shidrl@panfgh")]
    public async Task SendInviteAsync_InvalidEmail_ThrowsEmailInvalidException(string email)
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = []
        };

        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        _mockEmailQueue
            .QueueEmailAsync(new EmailJob("test@example.com", EmailTemplate.CollaborationInvite,
                new Dictionary<string, string>())).Returns(new ValueTask());
        
        await Should.ThrowAsync<EmailInvalidationException>(
            async () => await ServiceUnderTest.SendInviteAsync(email, 1, 1)
        );
    }

    [Fact]
    public async Task SendInviteAsync_InviteSentToOwner_UserInTripException()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = []
        };

        context.Trips.Add(trip);

        await context.SaveChangesAsync();

        _mockEmailQueue
            .QueueEmailAsync(new EmailJob("test@example.com", EmailTemplate.CollaborationInvite,
                new Dictionary<string, string>())).Returns(new ValueTask());
        
        var err = await Should.ThrowAsync<UserInTripException>(
            async () => await ServiceUnderTest.SendInviteAsync("test@example.com", 1, 1)
        );
        
        err.Message.ShouldBe("You cannot invite yourself to a trip.");
    }

    [Fact]
    public async Task SendInviteAsync_UserAlreadyInvited_UserInTripException()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        User user3 = new()
        {
            Id = 3,
            Email = "test3@example.com",
            DisplayName = "Test User 3",
            PasswordKey = "password",
            Country = "Test Country"
        };
        
        context.Users.Add(user2);
        context.Users.Add(user3);
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2, 3]
        };

        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        _mockEmailQueue
            .QueueEmailAsync(new EmailJob("test@example.com", EmailTemplate.CollaborationInvite,
                new Dictionary<string, string>())).Returns(new ValueTask());
        
        var err = await Should.ThrowAsync<UserInTripException>(
            async () => await ServiceUnderTest.SendInviteAsync("test2@example.com", 1, 1)
        );
        
        err.Message.ShouldBe("This user is already a part of this trip.");
    }

    [Fact]
    public async Task SendInvitesAsync_TripBelongsToPersonBeingInvited_UserInTripException()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 2,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = []
        };
        

        context.Users.Add(user2);

        context.Trips.Add(trip);

        await context.SaveChangesAsync();

        _mockEmailQueue
            .QueueEmailAsync(new EmailJob("test@example.com", EmailTemplate.CollaborationInvite,
                new Dictionary<string, string>())).Returns(new ValueTask());
        
        var err = await Should.ThrowAsync<UserInTripException>(
            async () => await ServiceUnderTest.SendInviteAsync("test2@example.com", 1, 1)
        );

        err.Message.ShouldBe("This user is already a part of this trip.");
    }

    [Fact]
    public async Task SendInvitesAsync_ValidInvitation_EmailQueued()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        context.Users.Add(user2);
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = []
        };

        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        _mockEmailQueue
            .QueueEmailAsync(new EmailJob("test@example.com", EmailTemplate.CollaborationInvite,
                new Dictionary<string, string>())).Returns(new ValueTask());
        
        await Should.NotThrowAsync(
            async () => await ServiceUnderTest.SendInviteAsync("test2@example.com", 1, 1)
        );
    }

    [Fact]
    public async Task SendInvitesAsync_EmailNotRegisteredInPolaroam_EmailStillQueued()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = []
        };

        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        _mockEmailQueue
            .QueueEmailAsync(new EmailJob("test@example.com", EmailTemplate.CollaborationInvite,
                new Dictionary<string, string>())).Returns(new ValueTask());

        await Should.NotThrowAsync(async () => await ServiceUnderTest.SendInviteAsync("bob@bob.com", 1, 1));
    }

    [Fact]
    public async Task SendInviteAsync_InvitedUserAlreadyHasInvitePending_PendingInviteExceptionThrown()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [],
            PendingInvites = ["test2@example.com",]
        };
        
        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        context.Users.Add(user2);


        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        await Should.ThrowAsync<PendingInviteException>(async () => await ServiceUnderTest.SendInviteAsync("test2@example.com", 1, 1));
    }

    
    [Fact]
    public async Task SendInviteAsync_InviteSentToUser_NotificationCreatedForUser()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = []
        };
        
        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        context.Users.Add(user2);

        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        _mockEmailQueue
            .QueueEmailAsync(new EmailJob("test2@example.com", EmailTemplate.CollaborationInvite,
                new Dictionary<string, string>())).Returns(new ValueTask());

        await Should.NotThrowAsync(async () => await ServiceUnderTest.SendInviteAsync("test2@example.com", 1, 1));

        var notification = await context.Notifications.Where(x => x.UserId == 2).FirstOrDefaultAsync();

        notification.ShouldNotBeNull();
        notification.HeaderText.ShouldBe("New Invitation Request");
    }

    [Fact]
    public async Task SwapOwnership_ValidTripAndUser_OwnershipIsSwapped()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        context.Users.Add(user2);
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2]
        };
        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        await Should.NotThrowAsync(
            async () => await ServiceUnderTest.SwapOwnership(1, 2, 1)
        );
    }
    
    [Fact]
    public async Task SwapOwnership_InvalidTrip_KeyNotFoundExceptionThrown()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        context.Users.Add(user2);
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2]
        };
        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        var err = await Should.ThrowAsync<KeyNotFoundException>(
            async () => await ServiceUnderTest.SwapOwnership(1, 2, 67)
        );
        
        err.Message.ShouldBe("Trip does not exist");
    }

    [Fact]
    public async Task SwapOwnership_ValidTripAndUser_NotificationCreatedForNewOwner()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        context.Users.Add(user2);
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2]
        };
        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        await Should.NotThrowAsync(
            async () => await ServiceUnderTest.SwapOwnership(1, 2, 1)
        );
        
        var notification = await context.Notifications.Where(x => x.UserId == 2).FirstOrDefaultAsync();

        notification.ShouldNotBeNull();
        notification.HeaderText.ShouldBe("Granted Ownership of Trip!!");
    }
}