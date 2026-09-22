using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class NotificationControllerTests : BaseIntegrationTestFixture
{
    private readonly INotificationService _notificationService;
    private readonly NotificationController _controller;

    public NotificationControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory)
    {
        _notificationService = Substitute.For<INotificationService>();
        _controller = new NotificationController(_notificationService);

        SetAuthenticatedUser("1");
    }

    private void SetAuthenticatedUser(string? userId)
    {
        var claims = new List<Claim>();

        if (userId != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
    }

    private static Notification CreateNotification(
        int id,
        int userId,
        string header,
        bool readStatus = false)
    {
        return new Notification
        {
            Id = id,
            UserId = userId,
            HeaderText = header,
            BodyText = "body",
            ReadStatus = readStatus,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task GetAllNotifications_ValidUser_ReturnsNotifications()
    {
        var notifications = new List<Notification>
        {
            CreateNotification(1, 1, "notification"),
            CreateNotification(2, 1, "notification", readStatus: true)
        };

        _notificationService.GetNotificationsByUserIdAsync(1, 5).Returns(notifications);

        _notificationService.GetUnreadCount(1).Returns(1);
        _notificationService.UserHasMoreThan(1, 5).Returns(false);

        var result = await _controller.GetNotifications(null, 0);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<GetAllNotificationsResponse>();

        response.NotificationResponses.Count.ShouldBe(2);
        response.NotificationResponses[0].Id.ShouldBe(1);
        response.NotificationResponses[0].HeaderText.ShouldBe("notification");
        response.NotificationResponses[1].Id.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllNotifications_ValidUser_ReturnsUnreadCount()
    {
        _notificationService.GetNotificationsByUserIdAsync(1, 5).Returns([]);

        _notificationService.GetUnreadCount(1).Returns(4);
        _notificationService.UserHasMoreThan(1, 5).Returns(true);

        var result = await _controller.GetNotifications(null, 0);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<GetAllNotificationsResponse>();

        response.UnreadCount.ShouldBe(4);
        response.MoreExist.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllNotifications_CountNotGiven_GetsFiveNotifications()
    {
        _notificationService.GetNotificationsByUserIdAsync(1, 5).Returns([]);

        var result = await _controller.GetNotifications(null, 0);

        result.Result.ShouldBeOfType<OkObjectResult>();

        await _notificationService.Received(1).GetNotificationsByUserIdAsync(1, 5);

        await _notificationService.Received(1).UserHasMoreThan(1, 5);
    }

    [Fact]
    public async Task GetAllNotifications_CountGiven_GetsSpecifiedCount()
    {
        _notificationService.GetNotificationsByUserIdAsync(1, 10).Returns([]);

        var result = await _controller.GetNotifications(10, 0);

        result.Result.ShouldBeOfType<OkObjectResult>();

        await _notificationService.Received(1).GetNotificationsByUserIdAsync(1, 10);

        await _notificationService.Received(1).UserHasMoreThan(1, 10);
    }

    [Fact]
    public async Task GetAllNotifications_TripInvitationNotification_HasTripId()
    {
        var notification = new TripInvitationNotification
        {
            Id = 1,
            UserId = 1,
            TripId = 42,
            HeaderText = "Trip invitation",
            BodyText = "You have been invited to a trip",
            NotificationType = "TripInvitationNotification",
            Timestamp = DateTimeOffset.UtcNow
        };

        _notificationService.GetNotificationsByUserIdAsync(1, 5).Returns([notification]);

        var result = await _controller.GetNotifications(null, 0);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<GetAllNotificationsResponse>();

        response.NotificationResponses.Single().TripId.ShouldBe(42);
    }

    [Fact]
    public async Task GetAllNotifications_MissingUserClaim_ReturnsUnauthorized()
    {
        SetAuthenticatedUser(null);

        var result = await _controller.GetNotifications(null, 0);

        result.Result.ShouldBeOfType<UnauthorizedResult>();

        await _notificationService.DidNotReceive().GetNotificationsByUserIdAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task GetAllNotifications_InvalidUserClaim_ReturnsUnauthorized()
    {
        SetAuthenticatedUser("invalid-user-id");

        var result = await _controller.GetNotifications(null, 0);

        var unauthorizedResult = result.Result.ShouldBeOfType<UnauthorizedObjectResult>();

        unauthorizedResult.Value.ShouldBe("Invalid User Id");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetReadStatus_UserOwnsNotification_UpdatesStatus(
        bool status)
    {
        _notificationService.UserOwnsNotification(4, 1).Returns(true);

        var request = new SetReadStatusRequest
        {
            Status = status
        };

        var result = await _controller.SetReadStatus(4, request);

        result.ShouldBeOfType<OkResult>();

        await _notificationService.Received(1).SetNotificationStatusByIdAsync(4, status);
    }

    [Fact]
    public async Task SetReadStatus_UserDoesntOwnNotification_ReturnsUnauthorized()
    {
        _notificationService.UserOwnsNotification(4, 1).Returns(false);

        var request = new SetReadStatusRequest
        {
            Status = true
        };

        var result = await _controller.SetReadStatus(4, request);

        var unauthorizedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();

        unauthorizedResult.Value.ShouldBe(
            "You dont have permission to update this notification"
        );

        await _notificationService.DidNotReceive().SetNotificationStatusByIdAsync(Arg.Any<int>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task SetReadStatus_MissingUserClaim_ReturnsUnauthorized()
    {
        SetAuthenticatedUser(null);

        var request = new SetReadStatusRequest
        {
            Status = true
        };

        var result = await _controller.SetReadStatus(4, request);

        result.ShouldBeOfType<UnauthorizedResult>();

        await _notificationService.DidNotReceive().UserOwnsNotification(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task SetReadStatus_InvalidUserClaim_ReturnsUnauthorized()
    {
        SetAuthenticatedUser("invalid-user-id");

        var request = new SetReadStatusRequest
        {
            Status = true
        };

        var result = await _controller.SetReadStatus(4, request);

        var unauthorizedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();

        unauthorizedResult.Value.ShouldBe("Invalid User Id");

        await _notificationService.DidNotReceive().SetNotificationStatusByIdAsync(Arg.Any<int>(), Arg.Any<bool>());
    }
}