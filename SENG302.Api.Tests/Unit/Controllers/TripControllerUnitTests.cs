using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using SENG302.Api.Services.Email;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Services;

public class TripControllerUnitTests
{
    private ITripService _mockTripService;
    private IPostService _mockPostService;
    private IAlbumService _mockAlbumService;
    private IUserService _mockUserService;
    private ICollaborationService _mockCollaborationService;

    private TripController _controller;

    public TripControllerUnitTests()
    {
        _mockTripService = Substitute.For<ITripService>();
        _mockPostService = Substitute.For<IPostService>();
        _mockAlbumService = Substitute.For<IAlbumService>();
        _mockUserService = Substitute.For<IUserService>();
        _mockCollaborationService = Substitute.For<ICollaborationService>();

        _controller = new TripController(
            _mockTripService,
            _mockPostService,
            _mockAlbumService,
            _mockUserService,
            _mockCollaborationService
        );

        TestingPrincipalHelper.AddTestPrincipalToController(_controller);
    }

    /// <summary>
    /// Returns a basic list of collaborating users for mocking purposes
    /// </summary>
    private List<CollaboratingUser> GetCollaboratingUsersMock()
    {
        return new List<CollaboratingUser> {
            new() {DisplayName = "Name", UserId = 2},
            new() {DisplayName = "Name 2", UserId = 3}
        };
    }

    /// <summary>
    /// Returns a basic trip for mocking purposes
    /// </summary>
    private Trip GetTripMock()
    {
        return new Trip
        {
            Id = 1,
            UserId = 1,
            Title = "Trip",
            Description = "Description",
            EndDate = DateTimeOffset.UtcNow,
            StartDate = DateTimeOffset.UtcNow,
            Collaborators = [2, 3],
        };
    }

    private User GetUserMock()
    {
        return new User
        {
            Id = 1,
            DisplayName = "John Example",
            Email = "john@example.com",
            Country = "US",
        };
    }

    [Fact]
    public async Task GetTripCollaborators_ValidRequest_ReturnsCorrectly()
    {
        var mockObject = GetCollaboratingUsersMock();

        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Returns(true);
        _mockCollaborationService.GetTripCollaborators(1).Returns(mockObject);

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<OkObjectResult>();

        var collaborators = ((OkObjectResult)response.Result).Value;
        collaborators.ShouldNotBeNull();
        collaborators.ShouldBe(mockObject);
    }

    [Fact]
    public async Task GetTripCollaborators_NoClaim_ReturnsUnauthorized()
    {
        TestingPrincipalHelper.RemoveTestPrincipalFromController(_controller);

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_ClaimHasGarbledId_ReturnsUnauthorized()
    {
        TestingPrincipalHelper.AddGarbledPrincipalToController(_controller);

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
    public async Task TripToTripResponse_ValidInformation_DataIsCorrect()
    {
        var mockTrip = GetTripMock();
        var mockCollaborators = GetCollaboratingUsersMock();
        var mockUser = GetUserMock();

        _mockUserService.GetUserByIdAsync(1).Returns(mockUser);
        _mockCollaborationService.GetTripCollaborators(1).Returns(mockCollaborators);

        var checking = await _controller.TripToTripResponse(mockTrip);

        checking.Id.ShouldBe(mockTrip.Id);
        checking.UserId.ShouldBe(mockTrip.UserId);
        checking.OwnerName.ShouldBe(mockUser.DisplayName);
        checking.Title.ShouldBe(mockTrip.Title);
        checking.Description.ShouldBe(mockTrip.Description);
        checking.StartDate.ShouldBe(mockTrip.StartDate);
        checking.EndDate.ShouldBe(mockTrip.EndDate);
        checking.Collaborators.ShouldBe(mockCollaborators);
    }

    [Fact]
    public async Task TripToTripResponse_NoUser_ReturnsKeyNotFound()
    {
        var mockTrip = GetTripMock();

        _mockUserService.GetUserByIdAsync(1).Returns(Task.FromResult<User?>(null));

        _controller.TripToTripResponse(mockTrip).ShouldThrow<KeyNotFoundException>();
    }

}
