using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U70022Notifications
{
    private readonly AcceptanceTestFixture _fixture;
    private readonly DateTimeOffset _testTime = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private int _notificationId;
    private int _initialNotificationCount;
    private GetAllNotificationsResponse? _notificationResponse;
    private HttpResponseMessage? _lastResponse;

    public U70022Notifications(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    private Notification CreateNotification(
        string header,
        bool readStatus,
        DateTimeOffset timestamp)
    {
        return new Notification
        {
            UserId = 1,
            HeaderText = header,
            BodyText = "body",
            ReadStatus = readStatus,
            Timestamp = timestamp
        };
    }

    private async Task AddNotifications(params Notification[] notifications)
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();
    }

    private async Task GetNotifications(int count)
    {
        _lastResponse = await _fixture.HttpClient.GetAsync(
            $"api/user/notifications?limit={count}"
        );

        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        _notificationResponse = await _lastResponse.Content.ReadFromJsonAsync<GetAllNotificationsResponse>();
        _notificationResponse.ShouldNotBeNull();
    }

    [Given("I am logged in and have read and unread notifications")]
    public async Task GivenIHaveReadAndUnreadNotifications()
    {
        await AddNotifications(
            CreateNotification(
                "Older unread",
                false,
                _testTime.AddMinutes(-2)
            ),
            CreateNotification(
                "Newest read",
                true,
                _testTime
            ),
            CreateNotification(
                "Newest unread",
                false,
                _testTime.AddMinutes(-1)
            ),
            CreateNotification(
                "Older read",
                true,
                _testTime.AddMinutes(-3)
            )
        );
    }

    [Given("I am logged in and have more than five notifications")]
    public async Task GivenIHaveMoreThanFiveNotifications()
    {
        var notifications = Enumerable.Range(1, 7).Select(number => CreateNotification(
                $"Notification {number}",
                false,
                _testTime.AddMinutes(-number)
            )).ToArray();

        await AddNotifications(notifications);
    }

    [Given("I am logged in and have an unread notification")]
    public async Task GivenIHaveAnUnreadNotification()
    {
        var notification = CreateNotification(
            "Unread notification",
            false,
            _testTime
        );

        await AddNotifications(notification);
        _notificationId = notification.Id;
    }

    [Given("I am logged in and have three unread notifications and one read notification")]
    public async Task GivenIHaveThreeUnreadNotificationsAndOneReadNotification()
    {
        await AddNotifications(
            CreateNotification("Unread one", false, _testTime),
            CreateNotification("Unread two", false, _testTime.AddMinutes(-1)),
            CreateNotification("Unread three", false, _testTime.AddMinutes(-2)),
            CreateNotification("Read notification", true, _testTime.AddMinutes(-3))
        );
    }

    [Given("I am logged in and have a read notification")]
    public async Task GivenIHaveAReadNotification()
    {
        var notification = CreateNotification(
            "Read notification",
            true,
            _testTime
        );

        await AddNotifications(notification);
        _notificationId = notification.Id;
    }

    [When("I open the notification menu")]
    public async Task WhenIOpenTheNotificationMenu()
    {
        await GetNotifications(5);
        _initialNotificationCount = _notificationResponse!
            .NotificationResponses.Count;
    }

    [When("I request more notifications")]
    public async Task WhenIRequestMoreNotifications()
    {
        await GetNotifications(10);
    }

    [When("I mark the notification as read")]
    public async Task WhenIMarkTheNotificationAsRead()
    {
        _lastResponse = await _fixture.HttpClient.PutAsJsonAsync(
            $"api/user/notifications/{_notificationId}/status",
            new SetReadStatusRequest { Status = true }
        );
    }

    [When("I mark the notification as unread")]
    public async Task WhenIMarkTheNotificationAsUnread()
    {
        _lastResponse = await _fixture.HttpClient.PutAsJsonAsync(
            $"api/user/notifications/{_notificationId}/status",
            new SetReadStatusRequest { Status = false }
        );
    }

    [Then("I see unread notifications before read notifications")]
    public void ThenISeeUnreadNotificationsBeforeReadNotifications()
    {
        var notifications = _notificationResponse!.NotificationResponses;

        notifications.Select(notification => notification.ReadStatus).ShouldBe([false, false, true, true]);
    }

    [Then("the notifications in each section are newest first")]
    public void ThenTheNotificationsInEachSectionAreNewestFirst()
    {
        var notifications = _notificationResponse!.NotificationResponses;

        notifications.Select(notification => notification.HeaderText).ShouldBe([
                "Newest unread",
                "Older unread",
                "Newest read",
                "Older read"
            ]);
    }

    [Then("more notifications are loaded")]
    public void ThenMoreNotificationsAreLoaded()
    {
        _initialNotificationCount.ShouldBe(5);
        _notificationResponse!.NotificationResponses.Count.ShouldBe(7);
        _notificationResponse.MoreExist.ShouldBeFalse();
    }

    [Then("the notification is marked as read")]
    public async Task ThenTheNotificationIsMarkedAsRead()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var notification = await context.Notifications
            .SingleAsync(notification => notification.Id == _notificationId);

        notification.ReadStatus.ShouldBeTrue();
    }

    [Then("the unread notification count is three")]
    public void ThenTheUnreadNotificationCountIsThree()
    {
        _notificationResponse!.UnreadCount.ShouldBe(3);
    }

    [Then("the notification is marked as unread")]
    public async Task ThenTheNotificationIsMarkedAsUnread()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var notification = await context.Notifications
            .SingleAsync(notification => notification.Id == _notificationId);

        notification.ReadStatus.ShouldBeFalse();
    }
}
