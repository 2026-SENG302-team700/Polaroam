using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using static SENG302.Api.Helpers.DateHelper;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Controllers;

public class CollaborationControllerTests : BaseIntegrationTestFixture
{
    private ITestOutputHelper _output;

    private readonly ITripService _mockTripService;
    private readonly ICollaborationService _mockCollaborationService;
    private readonly IUserService _mockUserService;

    private readonly CollaborationController _controller;

    public CollaborationControllerTests(WebApplicationFactory<Program> webApplicationFactory, ITestOutputHelper output) :
            base(webApplicationFactory)
    {
        _mockTripService = Substitute.For<ITripService>();
        _mockCollaborationService = Substitute.For<ICollaborationService>();
        _mockUserService = Substitute.For<IUserService>();

        _controller = new CollaborationController(
            _mockTripService,
            _mockCollaborationService,
            _mockUserService
        );

        TestingPrincipalHelper.AddTestPrincipalToController(_controller);

        _output = output;
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
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(testUser);
        await context.SaveChangesAsync();

        _mockUserService.GetUserByIdAsync(testUser.Id).Returns(Task.FromResult((User?)testUser));
    }

    [Fact]
    public async Task GetTripCollaborators_ValidRequest_ReturnsCorrectly()
    {
        List<CollaboratingUserResponse> mockObject = [
            new() {DisplayName = "Name", UserId = 2},
            new() {DisplayName = "Name 2", UserId = 3}
        ];

        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Returns(true);
        _mockCollaborationService.GetTripCollaborators(1).Returns(mockObject);

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<OkObjectResult>();

        var collaborators = ((OkObjectResult)response.Result).Value;
        collaborators.ShouldNotBeNull();
        collaborators.ShouldBe(mockObject);
    }

    [Fact]
    public async Task GetTripCollaborators_NoCollaborators_ReturnsOkResponseWithEmptyList()
    {
        await AddTestUser();

        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = []
        };

        await using var context = DbContextFactory.CreateDbContext();

        context.Trips.Add(trip);

        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/trip/1/collaboration");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var collaborators = await response.Content.ReadFromJsonAsync<List<CollaboratingUserResponse>>();

        collaborators.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTripCollaborators_NoClaim_ReturnsUnauthorized()
    {

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_ClaimHasGarbledId_ReturnsUnauthorized()
    {

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "bad-id") };
        var identity = new ClaimsIdentity(claims, "IncorrectAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_TripDoesNotExist_ReturnsNotFound()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Throws<KeyNotFoundException>();

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_UserDoesNotExist_ReturnsNotFound()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Throws<ArgumentException>();

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_UserHasNoAccess_ReturnsNotFound()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Returns(false);

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task LeaveCollaboration_ValidRequest_ReturnsNoContent()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Returns(true);

        var response = await _controller.LeaveCollaboration(1);
        response.ShouldBeOfType<NoContentResult>();
    }
    
    [Fact]
    public async Task LeaveCollaboration_NotCollaborator_ReturnsUnauthorized()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Returns(false);
        
        var response = await _controller.LeaveCollaboration(1);
        response.ShouldBeOfType<UnauthorizedObjectResult>();
    }
    
    [Fact]
    public async Task LeaveCollaboration_ErrorOccurs_ReturnsBadRequest()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Throws(new Exception());
        
        var response = await _controller.LeaveCollaboration(1);
        response.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task InviteCollaborator_UserNotLoggedIn_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext                                                                                                                      
        {                                                                                                                                                                          
            HttpContext = new DefaultHttpContext()                                                                                                                                 
        };   
        
        var requestData = new InviteCollaboratorRequest { Email = "test@example.com"};
       
        var response = await _controller.InviteUser(1, requestData);
        
        response.ShouldBeOfType<UnauthorizedObjectResult>();

        var responseObject = response as UnauthorizedObjectResult;

        var CollaborationResponse = responseObject?.Value as CollaborationInviteResponse;
        
        CollaborationResponse?.Message.ShouldBe("Not logged in");

        // responseObject?.Value?.ToString().ShouldBe("Not logged in");
    }

    [Fact]
    public async Task InviteCollaborator_UserDoesNotHaveAccessToTrip_ReturnsUnauthorized()
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
            UserId = 2,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [3]
        };
        
        context.Trips.Add(trip);

        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/trip/1/collaboration");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("User does not have access to collaborate on the trip and view collaboration details");
    }

    [Fact]
    public async Task InviteCollaborator_EmailInvalid_ReturnsBadRequest()
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
            Collaborators = [3]
        };
        
        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        var requestData = new InviteCollaboratorRequest { Email = "test@@example.com"};

        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration", requestData);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        
        var colabResponse = await  response.Content.ReadFromJsonAsync<CollaborationInviteResponse>();
        colabResponse?.Message.ShouldBe("Invalid email address. Email must be in the format 'jane@doe.nz'");
    }
    
    [Fact]
    public async Task InviteCollaborator_UserInTrip_ReturnsBadRequest()
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
        
        var requestData = new InviteCollaboratorRequest { Email = "test2@example.com"};

        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration", requestData);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var colabResponse = await  response.Content.ReadFromJsonAsync<CollaborationInviteResponse>();
        colabResponse?.Message.ShouldBe("This user is already a part of this trip.");
        
    }

    [Fact]
    public async Task InviteCollaborator_UserNotInTrip_ReturnsOK()
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
        
        var requestData = new InviteCollaboratorRequest { Email = "test2@example.com"};

        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration", requestData);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InviteCollaborator_UserAlreadySentInvite_ReturnsBadRequest()
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
            Collaborators = [],
            PendingInvites = ["test2@example.com"]
        };
        
        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        var requestData = new InviteCollaboratorRequest { Email = "test2@example.com"};

        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration", requestData);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InviteCollaborator_UserNotInTrip_UserRecievesNotification()
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
        
        var requestData = new InviteCollaboratorRequest { Email = "test2@example.com"};

        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration", requestData);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var notification = await context.Notifications.Where(n => n.UserId == 2 && n.NotificationType == "TripInvitation").FirstOrDefaultAsync();
        notification.ShouldNotBeNull();
        notification.HeaderText.ShouldBe("New Invitation Request");
        notification.BodyText.ShouldBe("You have been invited to collaborate on the Trip! trip!");
    }
    
    [Fact] 
    public async Task ActionCollaborator_AcceptInvite_ReturnsOK()
    {
        // Setup
        await AddTestUser(); // Id 1, Email: test@example.com
        await using var context = await DbContextFactory.CreateDbContextAsync();
        User user2 = new User
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
            UserId = 2,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [],
            PendingInvites = ["test@example.com"]
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();
    
        // Accept Invite
        CollaborationRequest request = new CollaborationRequest
        {
            Action = 1
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration/action", request);
        
        // Validate response
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("successfully accepted");
    }
    
    [Fact] 
    public async Task ActionCollaborator_RejectInvite_ReturnsOK()
    {
        // Setup
        await AddTestUser(); // Id 1, Email: test@example.com
        await using var context = await DbContextFactory.CreateDbContextAsync();
        User user2 = new User
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
            UserId = 2,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [],
            PendingInvites = ["test@example.com"]
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();
    
        // Reject Invite
        CollaborationRequest request = new CollaborationRequest
        {
            Action = 0
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration/action", request);
        
        // Validate response
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("successfully rejected");
    }
    
    [Fact] 
    public async Task ActionCollaborator_UserNotFound_ReturnsUnauthorized()
    {
        // Accept Invite
        CollaborationRequest request = new CollaborationRequest
        {
            Action = 0
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration/action", request);
        
        // Validate response
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("User not found");
    }
    
    [Fact] 
    public async Task ActionCollaborator_NoInvite_ReturnsBadRequest()
    {
        // Setup
        await AddTestUser(); // Id 1, Email: test@example.com
        await using var context = await DbContextFactory.CreateDbContextAsync();
        User user2 = new User
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
            UserId = 2,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [],
            PendingInvites = []
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();
    
        // Accept Invite
        CollaborationRequest request = new CollaborationRequest
        {
            Action = 1
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration/action", request);
        
        // Validate response
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("No pending invites");
    }
    
    [Fact] 
    public async Task ActionCollaborator_InvalidAction_ReturnsBadRequest()
    {
        // Setup
        await AddTestUser(); // Id 1, Email: test@example.com
        await using var context = await DbContextFactory.CreateDbContextAsync();
        User user2 = new User
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
            UserId = 2,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [],
            PendingInvites = ["test@example.com"]
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();
    
        // Invalid Action
        CollaborationRequest request = new CollaborationRequest
        {
            Action = 2
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip/1/collaboration/action", request);
        
        // Validate response
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("Not a valid collaboration action!");
    }

    [Fact]
    public async Task KickCollaborator_UserIsCurrentlyCollaborator_UserIsKickedAndOKResponseRecieved()
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
        
        var response = await HttpClient.DeleteAsync("/api/trip/1/collaboration/kick/2");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var updatedContext = DbContextFactory.CreateDbContext();
        Trip? updatedTrip = await updatedContext.Trips.FirstOrDefaultAsync(t => t.Id == 1);
        updatedTrip.ShouldNotBeNull();
        updatedTrip.Collaborators.ShouldNotContain(2);
    }

    [Fact]
    public async Task KickCollaborator_TripDoesNotExist_BadRequestReturned()
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
        
        var response = await HttpClient.DeleteAsync("/api/trip/67/collaboration/kick/2");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        String message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("Trip with the provided ID does not exist.");
    }
    
    [Fact]
    public async Task KickCollaborator_KickedUserDoesNotExist_BadRequestReturned()
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
            Collaborators = [2]
        };
        
        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        var response = await HttpClient.DeleteAsync("/api/trip/1/collaboration/kick/2");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        String message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("User does not exist");
    }

    [Fact]
    public async Task KickCollaborator_KickedUserNotPartOfTrip_ReturnsUnauthorized()
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
            UserId = 2,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = []
        };
        
        context.Trips.Add(trip);

        await context.SaveChangesAsync();
        
        var response = await HttpClient.DeleteAsync("/api/trip/1/collaboration/kick/2");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        String message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("User does not have permission to kick collaborators from this trip");
    }
    
    [Fact]
    public async Task KickCollaborator_UserNotLoggedIn_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext                                                                                                                      
        {                                                                                                                                                                          
            HttpContext = new DefaultHttpContext()                                                                                                                                 
        };   

        var response = await _controller.KickUser(1, 2);

        response.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }
    
    [Fact]
    public async Task SwapOwnership_ToCollaborator_ReturnsOK()
    {
        // Setup: User ID 1 is the owner of trip 1, and user ID 2 is a collaborator
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
        
        // Swap Ownership
        var response = await HttpClient.PatchAsync("/api/trip/1/collaboration/transfer/2", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task SwapOwnership_NotOwner_ReturnsUnauthorized()
    {
        // Setup: User ID 2 is the owner of trip 1, and user ID 1 is a collaborator
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
        context.Users.Add(user2);
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 2,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [1]
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();
        
        // User ID 1 attempts to give themselves ownership of trip 1
        var response = await HttpClient.PatchAsync("/api/trip/1/collaboration/transfer/1", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("User cannot transfer ownership as they are not the current owner");
    }
    
    [Fact]
    public async Task SwapOwnership_ToNonCollaborator_ReturnsOK()
    {
        // Setup: User ID 1 is the owner of trip 1, and user ID 2 is not a collaborator but does exist
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
        
        // Attempt to swap ownership to user ID 2
        var response = await HttpClient.PatchAsync("/api/trip/1/collaboration/transfer/2", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("New owner does not have access to collaborate on the trip");
    }
    
    [Fact]
    public async Task SwapOwnership_TripDoesNotExist_ReturnsBadRequest()
    {
        // Setup: User ID 1 is the owner of trip 1, and user ID 2 is a collaborator
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
        context.Users.Add(user2);
        await context.SaveChangesAsync();
        
        // Swap Ownership
        var response = await HttpClient.PatchAsync("/api/trip/1/collaboration/transfer/2", null);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var message = await response.Content.ReadAsStringAsync();
        message.ShouldBe("Trip with the provided ID does not exist.");
    }
}