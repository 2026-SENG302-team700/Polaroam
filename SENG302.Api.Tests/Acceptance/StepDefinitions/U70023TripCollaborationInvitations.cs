using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Tests.Acceptance.Setup;
using static SENG302.Api.Helpers.DateHelper;
using Shouldly;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U70023TripCollaborationInvitations
{
    private readonly AcceptanceTestFixture _fixture;
    private readonly DateTimeOffset _testTime = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
    
    private GetAllNotificationsResponse? _notificationResponse;
    private HttpResponseMessage? _lastResponse;

    public U70023TripCollaborationInvitations(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    public async Task SetupUser()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        
        await context.SaveChangesAsync();
    }

    [Given("I am a user of Polaroam")]
    public async Task GivenIAmAUserOfPolaroam()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
        {
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User2",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();
    }

    [When("I am sent a collaboration invite")]
    public async Task WhenIAmSentACollaborationInvite()
    {
        
        await SetupUser();
        
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        
        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "Original trip",
            Description = "Original description",
            StartDate = DateToDateTimeOffset(2026, 7, 1),
            EndDate = DateToDateTimeOffset(2026, 7, 10),
            UserId = 1
        });
        
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });

        await context.SaveChangesAsync();
        
        var requestData = new InviteCollaboratorRequest { Email = "test2@example.com"};
        var response = await _fixture.HttpClient.PostAsJsonAsync("/api/trip/1/collaboration", requestData);
    }

    [Then(
        "I receive a notification with options to accept or decline and a email letting me know I have been invited to the specific trip")]
    public async Task ThenIRecieveANotification()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();

        var notification = await context.Notifications.Where(n => n.UserId == 2 && n.NotificationType == "TripInvitation")
            .FirstOrDefaultAsync();
        notification.ShouldNotBeNull();
        notification.HeaderText.ShouldBe("New Invitation Request");
        notification.BodyText.ShouldBe("You have been invited to collaborate on the Original trip trip!");
    }

}