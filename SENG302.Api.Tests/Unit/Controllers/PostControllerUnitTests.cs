using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Controllers;

public class PostControllerUnitTests
{
    private readonly IPostService _mockPostService;
    private readonly IUserService _mockUserService;
    private readonly IFileService _mockFileService;
    private readonly ITripService _mockTripService;
    private readonly PostController _controller;

    public PostControllerUnitTests()
    {
        _mockPostService = Substitute.For<IPostService>();
        _mockUserService = Substitute.For<IUserService>();
        _mockFileService = Substitute.For<IFileService>();
        _mockTripService = Substitute.For<ITripService>();

        _controller = new PostController(
            _mockPostService,
            _mockUserService,
            _mockFileService,
            _mockTripService
        );
    }

    private void SetupUserContext(string userId)
    {
        var claims = new[]
        {
              new Claim(ClaimTypes.NameIdentifier, userId)
          };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task DeletePost_UserOwnsPost_DeletesPostAndReturnsNoContent()
    {
        SetupUserContext("1");

        var post = new Post
        {
            PostId = 10,
            TripId = 20,
            Title = "Post to delete",
            Content = "",
            OsmId = 123,
            PhotoIds = []
        };

        _mockPostService.GetPostAsync(10).Returns(post);
        _mockPostService.UserOwnsPost(1, 20).Returns(true);

        var result = await _controller.DeletePost(10);

        result.ShouldBeOfType<NoContentResult>();
        await _mockPostService.Received(1).DeletePostByIdAsync(10);
    }
}