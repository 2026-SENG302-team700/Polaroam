using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using NSubstitute;
using Shouldly;
using SENG302.Api.Controllers.UserController.PasswordController;
using Microsoft.AspNetCore.Http.HttpResults;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.Email;

namespace SENG302.Api.Tests.Unit.Controllers;

public class ResetPasswordUnitTests
{
    private readonly IUserService _mockUserService;
    private readonly IOneTimeCodeService _mockCodeService;
    private readonly IEmailQueue _mockEmailQueue;
    private readonly ResetPasswordController _controller;
    private readonly IAuthenticationService _mockAuthService;

    public ResetPasswordUnitTests()
    {
        _mockUserService = Substitute.For<IUserService>();
        _mockCodeService = Substitute.For<IOneTimeCodeService>();
        _mockEmailQueue = Substitute.For<IEmailQueue>();
        _mockAuthService = Substitute.For<IAuthenticationService>();

        _controller = new ResetPasswordController(
            _mockUserService,
            _mockCodeService,
            _mockEmailQueue);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IAuthenticationService)).Returns(_mockAuthService);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
        };
    }

    /// <summary>
    /// Helper method to show a user who has Forgot Password by injecting the cookie claims into the controller
    /// </summary>
    private void SetupForgotPasswordContext(string email, long codeExpirationTime, string code, bool codeExpired)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Expiration, codeExpirationTime.ToString()),
            new Claim(ClaimTypes.PostalCode, code)
        };

        var principle = new ClaimsPrincipal(
            new ClaimsPrincipal(
                new ClaimsIdentity(claims, "TestPasswordResetScheme")
            )
        );

        // Code from Line 73 - 84 is attributed to Claude Code
        _mockAuthService
            .SignInAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<ClaimsPrincipal>(),
                Arg.Any<AuthenticationProperties>())
            .Returns(Task.CompletedTask);

        _mockAuthService
            .SignOutAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<AuthenticationProperties>())
            .Returns(Task.CompletedTask);

        if (codeExpired)
        {
            _mockAuthService.AuthenticateAsync(Arg.Any<HttpContext>(), "PasswordResetScheme")
                .Returns(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "PasswordResetScheme")));
        }
        else
        {
            _mockAuthService.AuthenticateAsync(Arg.Any<HttpContext>(), "PasswordResetScheme")
                .Returns(AuthenticateResult.Success(new AuthenticationTicket(principle, "PasswordResetScheme")));
        }


        var serviceProvider = new ServiceCollection()
            .AddSingleton(_mockAuthService)
            .BuildServiceProvider();

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principle,
                RequestServices = serviceProvider  // <-- this is what was missing
            }
        };
    }

    [Fact]
    public async Task GenerateResetPasswordCode_ValidEmail_ReturnsOk()
    {
        var request = new NewOneTimeCodeRequest { Email = "test@test.com" };
        var mockUser = new User { Email = "test@test.com", DisplayName = "test", Country = "NZ" };
        string code = "111222";

        _mockUserService.GetUserFromEmailAsync(request.Email).Returns(mockUser);
        _mockCodeService.GetEpochTime().Returns(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        _mockCodeService.GenerateOneTimeCode().Returns(code);

        var emailDictionary = new Dictionary<string, string>
        {
            {"MINUTES", "5"},
            {"CODE", code}
        };
        SetupForgotPasswordContext(request.Email, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), code, false);

        var result = await _controller.GenerateResetPasswordCode(request);
        result.Result.ShouldBeOfType<OkResult>();

        await _mockEmailQueue.Received(1).QueueEmailAsync(
            Arg.Is<EmailJob>(job =>
                job.toEmail == "test@test.com" &&
                job.Template == EmailTemplate.ChangePasswordCode &&
                job.Model["MINUTES"] == "5" &&
                job.Model["CODE"] == code),
            Arg.Any<CancellationToken>()
        );
    }


    [Fact]
    public async Task ValidateResetPasswordCode_ValidEmailAndCode_ReturnsOk()
    {
        var request = new ValidateOneTimeCodeRequest { Email = "test@test.com", Code = "111222" };
        var user = new User { DisplayName = "Test", Email = "test@test.com", Country = "NZ" };

        _mockUserService.GetUserFromEmailAsync(request.Email).Returns(user);
        _mockCodeService.GetEpochTime().Returns(1_000L);
        _mockCodeService.VerfiyCode(user, Arg.Any<long>(), request.Code)
            .Returns(CodeVerificationResult.CodeSuccessful);
        _mockUserService.UpdateUserOneTimeCode(user.Email, "", 0, true, user.ForgotIsResettingPassword).Returns(user);

        var result = await _controller.ValidateResetPasswordCode(request);

        result.Result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task ValidateResetPasswordCode_IncorrectEmail_ReturnsBadRequest()
    {
        var request = new ValidateOneTimeCodeRequest { Email = "test@test.com", Code = "111222" };

        _mockUserService.GetUserFromEmailAsync(request.Email).Returns((User)null);

        var result = await _controller.ValidateResetPasswordCode(request);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ValidateResetPasswordCode_IncorrectCode_ReturnsBadRequest()
    {
        var request = new ValidateOneTimeCodeRequest { Email = "test@test.com", Code = "111222" };
        var user = new User { DisplayName = "Test", Email = "test@test.com", Country = "NZ" };

        _mockUserService.GetUserFromEmailAsync(request.Email).Returns(user);
        _mockCodeService.GetEpochTime().Returns(1_000L);
        _mockCodeService.VerfiyCode(user, Arg.Any<long>(), request.Code)
            .Returns(CodeVerificationResult.CodeIncorrect);

        var result = await _controller.ValidateResetPasswordCode(request);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ValidateResetPasswordCode_TimedOutCode_ReturnsBadRequest()
    {
        var request = new ValidateOneTimeCodeRequest { Email = "test@test.com", Code = "111222" };
        var user = new User { DisplayName = "Test", Email = "test@test.com", Country = "NZ" };

        _mockUserService.GetUserFromEmailAsync(request.Email).Returns(user);
        _mockCodeService.GetEpochTime().Returns(1_000L);
        _mockCodeService.VerfiyCode(user, Arg.Any<long>(), request.Code)
            .Returns(CodeVerificationResult.CodeExpired);

        var result = await _controller.ValidateResetPasswordCode(request);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}