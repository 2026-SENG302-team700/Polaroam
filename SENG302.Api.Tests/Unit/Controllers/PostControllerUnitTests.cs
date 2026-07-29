using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using Shouldly;
using SENG302.Api.Models.Responses;
using SENG302.Api.Models.Requests;

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

    /// <summary>
    /// Create a new authenticated user
    /// </summary>
    /// <param name="userId"></param>
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

    /// <summary>
    /// Helper function that contains basic update request
    /// </summary>
    /// <returns></returns>
    private static UpdatePostRequest CreateUpdateRequest()
    {
        return new UpdatePostRequest
        {
            Id = 10,
            TripId = 20,
            Title = "Updated title",
            Content = "Updated content",
            OsmId = 123,
            Date = DateTimeOffset.Now.AddDays(-1)
        };
    }

    /// <summary>
    /// Helper function that contains basic create post request
    /// </summary>
    /// <returns></returns>
    private static NewPostRequest CreateNewPostRequest()
    {
        return new NewPostRequest
        {
            TripId = 20,
            Title = "New post",
            Content = "Post content",
            OsmId = 123,
            Date = DateTimeOffset.Now.AddDays(-1)
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
            OsmId = 1,
            LocationName = "Christchurch",
            PhotoIds = []
        };

        _mockPostService.GetPostAsync(10).Returns(post);
        _mockPostService.UserOwnsPost(1, 20).Returns(true);

        var result = await _controller.DeletePost(10);

        result.ShouldBeOfType<NoContentResult>();
        await _mockPostService.Received(1).DeletePostByIdAsync(10);
    }

    [Fact]
    public async Task CreatePost_ValidationFails_ReturnsValidationErrors()
    {
        SetupUserContext("1");

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test user",
            PasswordKey = "password",
            Country = "NZ"
        };

        var request = CreateNewPostRequest();

        var errors = new Dictionary<string, List<string>>
        {
            {
                "title",
                ["Title is required and must be between 3 and 128 characters long"]
            }
        };

        _mockUserService.GetUserByIdAsync(1).Returns(user);

        _mockPostService.CreateNewPostAsync(request, null, 1).Returns(Task.FromException<Post>(new MultipleValidationListException(errors)));

        var result = await _controller.CreatePost(request, null);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();

        var response = badRequest.Value.ShouldBeOfType<BadRequestValidationListResponse>();

        response.Errors.ShouldBe(errors);
    }

    [Fact]
    public async Task CreatePost_ServiceThrowsException_ReturnsBadRequest()
    {
        SetupUserContext("1");

        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test user",
            PasswordKey = "password",
            Country = "NZ"
        };

        var request = CreateNewPostRequest();

        _mockUserService.GetUserByIdAsync(1).Returns(user);

        _mockPostService.CreateNewPostAsync(request, null, 1).Returns(Task.FromException<Post>(new Exception("Unable to create post")));

        var result = await _controller.CreatePost(request, null);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();

        badRequest.Value.ShouldBe("Unable to create post");
    }

    [Fact]
    public async Task CreatePost_UserDoesNotExist_ReturnsUnauthorized()
    {
        SetupUserContext("1");

        var request = CreateNewPostRequest();

        _mockUserService.GetUserByIdAsync(1).Returns((User?)null);

        var result = await _controller.CreatePost(request, null);

        result.Result.ShouldBeOfType<UnauthorizedResult>();

        await _mockPostService.DidNotReceive().CreateNewPostAsync(Arg.Any<NewPostRequest>(), Arg.Any<List<IFormFile>?>(), Arg.Any<int>());
    }

    [Fact]
    public async Task GetPost_PostExists_ReturnsOkWithPost()
    {
        var post = new Post
        {
            PostId = 10,
            TripId = 20,
            Title = "Test post",
            Content = "Test content",
            OsmId = 1,
            LocationName = "Christchurch",
            PhotoIds = []
        };

        _mockPostService.GetPostAsync(10).Returns(post);

        var result = await _controller.GetPost(10);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(post);

        await _mockPostService.Received(1).GetPostAsync(10);
    }

    [Fact]
    public async Task UpdatePost_ServiceThrowsException_ReturnsBadRequest()
    {
        SetupUserContext("1");

        var request = CreateUpdateRequest();

        _mockPostService.UserOwnsPost(1, request.TripId).Returns(true);

        _mockPostService.UpdatePostAsync(request).Returns(Task.FromException<Post?>(new Exception("Unable to update post")));

        var result = await _controller.UpdatePost(request, null);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();

        badRequest.Value.ShouldBe("Unable to update post");
    }

    [Fact]
    public async Task GetPost_PostDoesNotExist_ReturnsNotFound()
    {
        _mockPostService.GetPostAsync(999).Returns((Post?)null);

        var result = await _controller.GetPost(999);

        result.Result.ShouldBeOfType<NotFoundResult>();

        await _mockPostService.Received(1).GetPostAsync(999);
    }

    [Fact]
    public async Task GetAllPosts_ServiceThrowsException_ReturnsBadRequest()
    {
        SetupUserContext("1");

        _mockPostService.GetAllPostsAsync(1).Returns(Task.FromException<IEnumerable<Post>>(new Exception("Unable to retrieve posts")));

        var result = await _controller.GetAllPosts();

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();

        badRequest.Value.ShouldBe("Unable to retrieve posts");
    }

    [Fact]
    public async Task GetAllPosts_MissingUserClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.GetAllPosts();

        result.Result.ShouldBeOfType<UnauthorizedResult>();

        await _mockPostService.DidNotReceive().GetAllPostsAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetPost_ServiceThrowsException_ReturnsBadRequest()
    {
        _mockPostService.GetPostAsync(10).Returns(Task.FromException<Post?>(new Exception("Unable to retrieve post")));

        var result = await _controller.GetPost(10);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();

        badRequest.Value.ShouldBe("Unable to retrieve post");
    }

    [Fact]
    public async Task UpdatePost_UserDoesNotOwnPost_ReturnsUnauthorized()
    {
        SetupUserContext("1");

        var request = CreateUpdateRequest();

        _mockPostService.UserOwnsPost(1, request.TripId).Returns(false);

        var result = await _controller.UpdatePost(request, null);

        result.Result.ShouldBeOfType<UnauthorizedResult>();

        await _mockPostService.Received(1).UserOwnsPost(1, request.TripId);

        await _mockPostService.DidNotReceive().UpdatePostAsync(Arg.Any<UpdatePostRequest>());
    }

    [Fact]
    public async Task UpdatePost_UserOwnsPost_ReturnsUpdatedPost()
    {
        SetupUserContext("1");

        var request = CreateUpdateRequest();

        var updatedPost = new Post
        {
            PostId = request.Id,
            TripId = request.TripId,
            Title = request.Title,
            Content = request.Content,
            OsmId = request.OsmId,
            LocationName = "Christchurch",
            Date = request.Date,
            PhotoIds = []
        };

        _mockPostService.UserOwnsPost(1, request.TripId).Returns(true);

        _mockPostService.UpdatePostAsync(request).Returns(updatedPost);

        var result = await _controller.UpdatePost(request, null);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(updatedPost);

        await _mockPostService.Received(1).UpdatePostAsync(request);
    }

    [Fact]
    public async Task UpdatePost_ValidationFails_ReturnsValidationErrors()
    {
        SetupUserContext("1");

        var request = CreateUpdateRequest();

        var errors = new Dictionary<string, List<string>>{
            {
                "location",
                ["This isn't a valid location"]
            }
        };

        _mockPostService.UserOwnsPost(1, request.TripId).Returns(true);

        _mockPostService.UpdatePostAsync(request).Returns(Task.FromException<Post?>(new MultipleValidationListException(errors)));

        var result = await _controller.UpdatePost(request, null);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();

        var response = badRequest.Value.ShouldBeOfType<BadRequestValidationListResponse>();

        response.Errors.ShouldBe(errors);
    }

    [Fact]
    public async Task DeletePost_MissingUserClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await _controller.DeletePost(10);

        result.ShouldBeOfType<UnauthorizedResult>();

        await _mockPostService.DidNotReceive().DeletePostByIdAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePost_PostDoesNotExist_ReturnsNotFound()
    {
        SetupUserContext("1");

        _mockPostService.GetPostAsync(999).Returns((Post?)null);

        var result = await _controller.DeletePost(999);

        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value.ShouldBe("Post does not exist");

        await _mockPostService.DidNotReceive().DeletePostByIdAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePost_UserDoesNotOwnPost_ReturnsUnauthorized()
    {
        SetupUserContext("1");

        var post = new Post
        {
            PostId = 10,
            TripId = 20,
            Title = "Someone else post",
            Content = "",
            OsmId = 1,
            LocationName = "Christchurch",
            PhotoIds = []
        };

        _mockPostService.GetPostAsync(10).Returns(post);
        _mockPostService.UserOwnsPost(1, 20).Returns(false);

        var result = await _controller.DeletePost(10);

        result.ShouldBeOfType<UnauthorizedResult>();

        await _mockPostService.DidNotReceive().DeletePostByIdAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePost_ServiceCannotFindPost_ReturnsNotFound()
    {
        SetupUserContext("1");

        var post = new Post
        {
            PostId = 10,
            TripId = 20,
            Title = "Test post",
            Content = "",
            OsmId = 1,
            LocationName = "Christchurch",
            PhotoIds = []
        };

        _mockPostService.GetPostAsync(10).Returns(post);
        _mockPostService.UserOwnsPost(1, 20).Returns(true);

        _mockPostService.DeletePostByIdAsync(10).Returns(Task.FromException(new ArgumentException("Post does not exist")));

        var result = await _controller.DeletePost(10);

        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value.ShouldBe("Post does not exist");
    }

    [Fact]
    public async Task DeletePhotoFromPost_UserOwnsPost_DeletesPostAndReturnsNoContent()
    {
        SetupUserContext("1");

        var post = new Post
        {
            PostId = 10,
            TripId = 20,
            Title = "Post to delete",
            Content = "",
            OsmId = 1,
            LocationName = "Christchurch",
            PhotoIds = [15]
        };
        
        _mockPostService.GetPostAsync(10).Returns(post);
        _mockPostService.UserOwnsPost(1, 20).Returns(true);

        var result = await _controller.DeletePhotoFromPost(10, 15);
        
        result.ShouldBeOfType<NoContentResult>();
        await _mockPostService.Received(1).DeletePhotoFromPostAsync(10, 15);
    }

    [Fact]
    public async Task DeletePhotoFromPost_PostDoesntExist_NotFound()
    {
        SetupUserContext("1");
        var result = await _controller.DeletePhotoFromPost(10, 15);
        
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeletePhotoFromPost_UserNotOwner_Unauthorized()
    {
        SetupUserContext("1");

        var post = new Post
        {
            PostId = 10,
            TripId = 20,
            Title = "Post to delete",
            Content = "",
            OsmId = 1,
            LocationName = "Christchurch",
            PhotoIds = [15]
        };
        
        _mockPostService.GetPostAsync(10).Returns(post);

        var result = await _controller.DeletePhotoFromPost(10, 15);
        
        result.ShouldBeOfType<UnauthorizedResult>();
    }
}