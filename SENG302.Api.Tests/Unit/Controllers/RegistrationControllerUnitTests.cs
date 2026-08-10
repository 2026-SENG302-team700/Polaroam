using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Services;
using NSubstitute;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using Shouldly;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Services.Email;

namespace SENG302.Api.Tests.Unit.Controllers;

public class RegistrationControllerUnitTests
{
    private readonly IUserService _mockUserService;
    private readonly IOneTimeCodeService _mockOneTimeCodeService;
    private readonly IEmailQueue _mockEmailQueue;
    private readonly RegistrationController _controller;

    public RegistrationControllerUnitTests()
    {
        _mockUserService = Substitute.For<IUserService>();
        _mockOneTimeCodeService = Substitute.For<IOneTimeCodeService>();
        _mockEmailQueue = Substitute.For<IEmailQueue>();
        _controller = new RegistrationController(_mockUserService, _mockOneTimeCodeService, _mockEmailQueue);

    }

    [Fact]
    public async Task GenerateCode_ValidEmail_ReturnOk()
    {
        long codeGenerationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 20;

        User user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country",
            OneTimeCode = "608975",
            CodeGenerationTime = codeGenerationTime,
            EmailVerified = false,
        };

        var emailDictionary = new Dictionary<string, string>
        {
            {"DISPLAY_NAME", user.DisplayName},
            {"CODE", user.OneTimeCode},
            {"MINUTES", "5"}
        };

        _mockOneTimeCodeService.GenerateOneTimeCode().Returns(user.OneTimeCode);
        _mockOneTimeCodeService.GetEpochTime().Returns(codeGenerationTime);
        _mockUserService.UpdateUserOneTimeCode(user.Email, user.OneTimeCode, codeGenerationTime, false).Returns(user);

        var data = new NewOneTimeCodeRequest
        {
            Email = user.Email,
        };
        // Call the controller directly
        var result = await _controller.initiateOneTimeCode(data);
        result.Result.ShouldBeOfType<OkResult>();

        await _mockEmailQueue.Received(1).QueueEmailAsync(
            Arg.Is<EmailJob>(job =>
                job.toEmail == user.Email &&
                job.Template == EmailTemplate.VerifyEmailCode &&
                job.Model["DISPLAY_NAME"] == user.DisplayName &&
                job.Model["CODE"] == user.OneTimeCode),
            Arg.Any<CancellationToken>()
        );
    }
}