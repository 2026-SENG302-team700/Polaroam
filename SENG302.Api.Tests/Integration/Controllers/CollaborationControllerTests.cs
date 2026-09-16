using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Controllers;

public class CollaborationControllerTests : BaseIntegrationTestFixture
{
    private ITestOutputHelper _output;

    private ITripService _mockTripService;
    private ICollaborationService _mockCollaborationService;

    private CollaborationController _controller;

    public CollaborationControllerTests(WebApplicationFactory<Program> webApplicationFactory, ITestOutputHelper output) :
            base(webApplicationFactory)
        {
            _mockTripService = Substitute.For<ITripService>();
            _mockCollaborationService = Substitute.For<ICollaborationService>();
    
            _controller = new CollaborationController(
                _mockTripService,
                _mockCollaborationService
            );
    
            AddTestPrincipalToController(_controller);
    
            _output = output;
        }
        
    [Fact]
    public async Task GetTripCollaborators_ValidRequest_ReturnsCorrectly()
    {
        List<CollaboratingUser> mockObject = [
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
}