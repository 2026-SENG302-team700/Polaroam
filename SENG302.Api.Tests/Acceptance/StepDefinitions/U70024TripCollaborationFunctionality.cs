using System.Net;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using SENG302.Api.Models.Responses;
using SENG302.Api.Tests.Acceptance.Setup;
using SENG302.Api.Models.Entities;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U70024TripCollaborationFunctionality
{
    private readonly AcceptanceTestFixture _fixture;
    private readonly DateTimeOffset _testTime = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
    
    private GetAllNotificationsResponse? _notificationResponse;
    private HttpResponseMessage? _lastResponse;
    
    public U70024TripCollaborationFunctionality(AcceptanceTestFixture fixture)
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

    [Given("I am viewing a confirm kick modal")]
    public async Task GivenIAmViewingAConfirmKickModal()
    {
        await SetupUser();
        
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        
        context.Users.Add(new User
        {
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User2",
            PasswordKey = "password",
            Country = "Test Country"
        });
        
        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "Original trip",
            Description = "Original description",
            StartDate = DateToDateTimeOffset(2026, 7, 1),
            EndDate = DateToDateTimeOffset(2026, 7, 10),
            UserId = 1,
            Collaborators = [2]
        });
        
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });

        await context.SaveChangesAsync();
    }

    [When("I press Confirm")]
    public async Task WhenIPressConfirm()
    {
        var response = await _fixture.HttpClient.DeleteAsync("/api/trip/1/collaboration/kick/2");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Then("the modal closes, that user is removed as a collaborator and can no longer contribute to the trip")]
    public async Task ThenThatUserIsRemovedAsCollaboratorAndCanNoLongerContributeToTheTrip()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();

        Trip? trip = await context.Set<Trip>().FirstOrDefaultAsync(t => t.Id == 1);

        trip?.Collaborators.ShouldBeEmpty();
    }

    [Given("I am collaborating on a trip")]
    public async Task GivenIAmCollaboratingOnATrip()
    {
        await SetupUser();
        
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        
        context.Users.Add(new User
        {
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User2",
            PasswordKey = "password",
            Country = "Test Country"
        });
        
        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "Original trip",
            Description = "Original description",
            StartDate = DateToDateTimeOffset(2026, 7, 1),
            EndDate = DateToDateTimeOffset(2026, 7, 10),
            UserId = 1,
            Collaborators = [2]
        });
        
        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });

        await context.SaveChangesAsync();
    }

    [When("I am kicked from the trip")]
    public async Task WheIAmKickedFromATrip()
    {
        var response = await _fixture.HttpClient.DeleteAsync("/api/trip/1/collaboration/kick/2");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Then("I receive a notification and email letting me know that I was kicked from the specific trip")]
    public async Task IRecieveANotificationAndEmailLettingMeKnowAboutBeingKickedFromATrip()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();

        Notification? notification = await context.Notifications.FirstOrDefaultAsync(n => n.UserId == 2);

        notification.ShouldNotBeNull();
        notification.HeaderText.ShouldBe("Kicked from Original trip!");
    }
}