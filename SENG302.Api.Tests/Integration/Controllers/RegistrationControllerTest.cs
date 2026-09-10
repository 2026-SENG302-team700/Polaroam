using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using SENG302.Api.Services.Email;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class RegistrationControllerTest : BaseIntegrationTestFixture
{
    private readonly IUserService _mockUserService;
    private readonly IOneTimeCodeService _mockOneTimeCodeService;
    private readonly IEmailQueue _mockEmailQueue;
    private readonly RegistrationController _controller;

    public RegistrationControllerTest(WebApplicationFactory<Program> webApplicationFactory) : base(
        webApplicationFactory)
    {
        _mockUserService = Substitute.For<IUserService>();
        _mockOneTimeCodeService = Substitute.For<IOneTimeCodeService>();
        _mockEmailQueue = Substitute.For<IEmailQueue>();
        _controller = new RegistrationController(_mockUserService, _mockOneTimeCodeService, _mockEmailQueue);
    }

    [Fact]
    public async Task RegisterUser_SuccessfulRegistration_ReturnOk()
    {
        var data = new PostUserRequest
        {
            Email = "great.person@gmail.com",
            DisplayName = "Great Person",
            PasswordString = "Gre@tPerson69",
            PasswordConfirm = "Gre@tPerson69",
            Country = "NZ",
        };

        var message = await HttpClient.PostAsJsonAsync("/api/register", data);

        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("shiv.sheep@gmail.com", "ShivSheep", "", "ES")]
    [InlineData("swag.mint@gmail.com", "SwagMintt", "Sheeep", "")]
    [InlineData("trad.horse@gmail.com", "", "TradTrad", "US")]
    [InlineData("", "Porcupine", "JohnPork", "US")]
    public async Task RegisterUser_MissingFields_ReturnBadRequest(string userEmail, string userDisplayName, string passwordString, string userCountry)
    {
        var data = new
        {
            Email = userEmail,
            DisplayName = userDisplayName,
            PasswordString = passwordString,
            PasswordConfirm = passwordString,
            Country = userCountry
        };

        var message = await HttpClient.PostAsJsonAsync("/api/register", data);

        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest); ;
    }


    [Fact]
    public async Task RegisterUser_SameEmailTwice_ReturnBadRequest()
    {
        var data = new
        {
            Email = "jdev@dev.com",
            DisplayName = "JJ Devy",
            PasswordString = "c00lPasSw0rdon't@ME",
            PasswordConfirm = "c00lPasSw0rdon't@ME",
            Country = "US"
        };

        var message = await HttpClient.PostAsJsonAsync("/api/register", data);

        message.StatusCode.ShouldBe(HttpStatusCode.OK);

        var data2 = new
        {
            Email = "jdev@dev.com",
            DisplayName = "JJ Devy second account",
            PasswordString = "c00lPasSw0rdon't@ME2",
            PasswordConfirm = "c00lPasSw0rdon't@ME2",
            Country = "US"
        };

        var message2 = await HttpClient.PostAsJsonAsync("/api/register", data2);
        message2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var content = await message2.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        var errors = json.GetProperty("errors");

        errors.GetProperty("email").GetString().ShouldContain("This email address is already in use by another account");
    }

    [Theory]
    [InlineData("   ")]
    public async Task GenerateCode_MissingEmail_ReturnBadRequest(string? userEmail)
    {
        var data = new
        {
            email = userEmail
        };
        var message = await HttpClient.PutAsJsonAsync("/api/register/code/generation", data);

        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var content = await message.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        json.GetProperty("message").GetString().ShouldBe("User email is missing");
    }


    [Fact]
    public async Task ValidateCode_IncorrectCode_ReturnBadRequest()
    {
        await using var context = DbContextFactory.CreateDbContext();

        long codeGenerationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 20;

        // Add user to DB
        context.Users.Add(new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country",
            OneTimeCode = "608975",
            CodeGenerationTime = codeGenerationTime,
        });
        await context.SaveChangesAsync();
        var data = new
        {
            email = "test@example.com",
            Code = "609809"
        };

        var message = await HttpClient.PostAsJsonAsync("/api/register/code/validation", data);

        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var content = await message.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        json.GetProperty("message").GetString().ShouldBe("Invalid Code");
    }


    [Fact]
    public async Task ValidateCode_InvalidTimedOutCode_ReturnBadRequest()
    {
        await using var context = DbContextFactory.CreateDbContext();

        long codeGenerationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600;

        // Add user to DB
        context.Users.Add(new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country",
            OneTimeCode = "608975",
            CodeGenerationTime = codeGenerationTime,
        });
        await context.SaveChangesAsync();
        var data = new
        {
            email = "test@example.com",
            Code = "609809"
        };

        var message = await HttpClient.PostAsJsonAsync("/api/register/code/validation", data);

        message.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var content = await message.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        json.GetProperty("message").GetString().ShouldBe("Code is no longer valid, account no longer exists");
    }

    [Fact]
    public async Task ValidateCode_ValidCode_ReturnOk()
    {
        await using var context = DbContextFactory.CreateDbContext();

        long codeGenerationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 20;

        // Add user to DB
        context.Users.Add(new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country",
            OneTimeCode = "608975",
            CodeGenerationTime = codeGenerationTime,
        });
        await context.SaveChangesAsync();
        var data = new
        {
            email = "test@example.com",
            Code = "608975"
        };

        var message = await HttpClient.PostAsJsonAsync("/api/register/code/validation", data);

        message.StatusCode.ShouldBe(HttpStatusCode.OK);
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
        _mockUserService.UpdateUserOneTimeCode(user.Email, user.OneTimeCode, codeGenerationTime, false, user.ForgotIsResettingPassword).Returns(user);

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