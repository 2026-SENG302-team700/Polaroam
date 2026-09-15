using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Controllers.UserController;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class UserControllerTests : BaseIntegrationTestFixture
{
    private readonly IOneTimeCodeService _mockOneTimeCodeService;
    private readonly IFileService _mockFileService;
    private readonly IAuthenticationService _mockAuthService;
    private readonly UserController _controller;

    private IUserService ServiceUnderTest => ServiceProvider.GetRequiredService<IUserService>();
    public UserControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory)
    {
        
        _mockOneTimeCodeService = Substitute.For<IOneTimeCodeService>();
        _mockFileService = Substitute.For<IFileService>();
        _mockAuthService = Substitute.For<IAuthenticationService>();
        _controller =
            new UserController(ServiceUnderTest, _mockFileService, _mockOneTimeCodeService);
    }


    private async Task AddTestUser()
    {
        // Add user to be updated to db
        await using var context = DbContextFactory.CreateDbContext();
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

    private void SetupUserContext(string userId, string name, string email)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Name, name), new Claim(ClaimTypes.Email, email) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
    
    private async Task SetupTestUser()
    {
        await AddTestUser();
        SetupUserContext("1", "Test User", "test@example.com");
    }

    private IFormFile GetMockFile(string filename, byte[] content, string mimeType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(), ContentType = mimeType
        };
    }
        
    [Fact]
    public async Task UpdateUser_Success_ReturnOk()
    {
        await AddTestUser();

        // Send update request
        var data = new
        {
            Email = "updated@example.com",
            DisplayName = "Updated User",
            Country = "US"
        };
        var response = await HttpClient.PutAsJsonAsync("/api/user", data);

        // Get updated context and verify
        await using var verifyContext = DbContextFactory.CreateDbContext();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == 1);
        updatedUser!.Email.ShouldBe("updated@example.com");
        updatedUser.DisplayName.ShouldBe("Updated User");
        updatedUser.Country.ShouldBe("US");
    }

    [Theory]
    [InlineData("", "test", "NZ")]
    [InlineData("updated@example.com", "", "NZ")]
    [InlineData("updated@example.com", "test", "")]
    public async Task UpdateUser_MissingField_BadRequest(string newEmail, string newDisplayName, string newCountry)
    {
        await AddTestUser();

        // Send update request
        var data = new
        {
            Email = newEmail,
            DisplayName = newDisplayName,
            Country = newCountry
        };
        var response = await HttpClient.PutAsJsonAsync("/api/user", data);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("test.userexample.com")]      // Missing @ symbol
    [InlineData("@example.com")]              // Missing username
    [InlineData("test.user@")]                // Missing domain
    [InlineData("test.user@example")]         // Missing top-level domain (TLD)
    [InlineData("t st.user@example.com")]     // Spaces
    [InlineData("te..st.user@example.com")]   // Consecutive periods
    [InlineData("test@user@example.com")]     // Multiple @ symbols
    [InlineData("test.user@gmail,com")]       // Missing dot in domain
    public async Task UpdateUser_MalformedEmail_BadRequest(string newEmail)
    {
        await AddTestUser();

        // Send update request
        var data = new
        {
            Email = newEmail,
            DisplayName = "Test",
            Country = "NZ"
        };
        var response = await HttpClient.PutAsJsonAsync("/api/user", data);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_EmailAlreadyExists_BadRequest()
    {
        await AddTestUser();

        // Add user to be updated to db
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 2,
            Email = "update@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        // Send update request
        var data = new
        {
            Email = "update@example.com",
            DisplayName = "Test",
            Country = "NZ"
        };
        var response = await HttpClient.PutAsJsonAsync("/api/user", data);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Hi")]
    [InlineData("Hi!")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 65 'a's
    public async Task UpdateUser_InvalidDisplayName_BadRequest(string newDisplayName)
    {
        await AddTestUser();

        // Send update request
        var data = new
        {
            Email = "update@example.com",
            DisplayName = newDisplayName,
            Country = "NZ"
        };
        var response = await HttpClient.PutAsJsonAsync("/api/user", data);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

    }
    
    [Fact]
    public async Task AcceptPrivacyPolicy_Success_UpdatesAndOk()
    {
        await AddTestUser();

        var response = await HttpClient.PutAsync("/api/user/accept-policy", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var verifyContext = DbContextFactory.CreateDbContext();
        var updatedUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == 1);
        updatedUser!.AcceptedAiPrivacyPolicy.ShouldBeTrue();
    }

    [Fact]
    public async Task ToggleAiTagging_PolicyAccepted_TogglesAndOk()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "vulad@nispor.md",
                DisplayName = "Vulad Nispor",
                PasswordKey = "P4$$word",
                Country = "MD",
                AcceptedAiPrivacyPolicy = true
            });
            await context.SaveChangesAsync();
        }

        var response = await HttpClient.PutAsync("/api/user/ai-toggle", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var verifyContext = DbContextFactory.CreateDbContext();
        var updatedUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == 1);
        updatedUser!.AiTaggingEnabled.ShouldBeTrue();
    }
    
    [Fact] 
    public async Task ToggleAiTagging_AiTaggingOn_TogglesAndOk()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "vulad@nispor.md",
                DisplayName = "Vulad Nispor",
                PasswordKey = "P4$$word",
                Country = "MD",
                AcceptedAiPrivacyPolicy = true,
                AiTaggingEnabled = true
            });
            await context.SaveChangesAsync();
        }
    
        var response = await HttpClient.PutAsync("/api/user/ai-toggle", null);
    
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    
        await using var verifyContext = DbContextFactory.CreateDbContext();
        var updatedUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == 1);
        updatedUser!.AiTaggingEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task ToggleAiTagging_ToggleTwice_AiTaggingDisabledAndOk()
    {
        await using (var context = DbContextFactory.CreateDbContext())
        {
            context.Users.Add(new User
            {
                Id = 1, 
                Email = "vulad@nispor.md",
                DisplayName = "Vulad Nispor",
                PasswordKey = "P4$$word",
                Country = "MD",
                AcceptedAiPrivacyPolicy = true
            });
            await context.SaveChangesAsync();
        }

        var response1 = await HttpClient.PutAsync("/api/user/ai-toggle", null);
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var response2 = await HttpClient.PutAsync("/api/user/ai-toggle", null);
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var verifyContext = DbContextFactory.CreateDbContext();
        var updatedUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == 1);
        updatedUser!.AiTaggingEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task ToggleAiTagging_PrivacyPolicyNotAccepted_ReturnsForbiddenNoAiToggle()
    {
        await AddTestUser();

        var response = await HttpClient.PutAsync("/api/user/ai-toggle", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await using var verifyContext = DbContextFactory.CreateDbContext();
        var updatedUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == 1);
        updatedUser!.AiTaggingEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task MapVisibility_SetX_IsX()
    {
        await AddTestUser();
        
        var dataTrue = new { MapVisible = true };
        var response1 = await HttpClient.PutAsJsonAsync("/api/user/preference/mapvisible", dataTrue);
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ServiceUnderTest.GetPreferenceMapVisibleAsync(1)).ShouldBeTrue();
        
        var dataFalse = new { MapVisible = false };
        var response2 = await HttpClient.PutAsJsonAsync("/api/user/preference/mapvisible", dataFalse);
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ServiceUnderTest.GetPreferenceMapVisibleAsync(1)).ShouldBeFalse();
    }

    [Fact]
    public async Task MapVisibility_IsX_GetX()
    {
        await AddTestUser();

        await ServiceUnderTest.SetPreferenceMapVisibleAsync(1, true);
        
        var response1 = await HttpClient.GetAsync("/api/user/preference/mapvisible");
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content1 = await response1.Content.ReadFromJsonAsync<MapVisibleObject>();
        content1.ShouldNotBeNull();
        content1.MapVisible.ShouldBeTrue();

        await ServiceUnderTest.SetPreferenceMapVisibleAsync(1, false);
        
        var response2 = await HttpClient.GetAsync("/api/user/preference/mapvisible");
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content2 = await response2.Content.ReadFromJsonAsync<MapVisibleObject>();
        content2.ShouldNotBeNull();
        content2.MapVisible.ShouldBeFalse();
    }
    
    

    [Fact]
    public async Task GetUser_UserExists_ReturnsOkWithNoPassword()
    {
        await SetupTestUser();
        var result = await _controller.GetUser();

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var user = okResult.Value.ShouldBeOfType<GetUserResponse>();
    }

    [Fact]
    public async Task GetUser_UserNotFound_ReturnsNotFound()
    {
        SetupUserContext("99","Test User", "test@example.com");

        var result = await _controller.GetUser();

        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUserVerificationCountdown_ValidTime_ReturnsRemainingSeconds()
    {
        var request = new NewOneTimeCodeRequest { Email = "test@test.com" };
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User{ Email = "test@test.com", DisplayName = "test", Country = "NZ", CodeGenerationTime = 1000 });
        await context.SaveChangesAsync();

        SetupUserContext("10", "testName", "test@test.com");
        
        _mockOneTimeCodeService.GetEpochTime().Returns(1050);
        _mockOneTimeCodeService.TimeoutTimeSeconds.Returns(300);

        var result = await _controller.GetUserVerificationCountdown(request);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(250L);
    }

    [Fact]
    public async Task GetUserVerificationCountdown_ExpiredCode_ReturnsUnauthorized()
    {
        var request = new NewOneTimeCodeRequest { Email = "test@test.com" };
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User{ Email = "test@test.com", DisplayName = "test", Country = "NZ", CodeGenerationTime = 1000 });
        await context.SaveChangesAsync();
        
        SetupUserContext("1", "testName", "test@test.com");
        
        _mockOneTimeCodeService.GetEpochTime().Returns(2000); // past timeout
        _mockOneTimeCodeService.TimeoutTimeSeconds.Returns(300);

        var result = await _controller.GetUserVerificationCountdown(request);

        result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    public async Task<IFormFile> CreateMockFile(byte[] bytes, string mimeType)
    {
        var stream = new MemoryStream(bytes);
        var mockfile = Substitute.For<IFormFile>();
        mockfile.ContentType.Returns(mimeType);
        mockfile.OpenReadStream().Returns(stream);

        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User{ Email = "test@test.com", Country = "NZ", DisplayName = "testName", Id = 10, ProfilePicture = 50 });
        await context.SaveChangesAsync();
        var oldFile = new CustomFile { Id = 50, OwnerId = 10, FileKey = "old-key", OriginalFileName = "old", MimeType = mimeType };
        var newFile = new CustomFile { Id = 101, OwnerId = 10, FileKey = "new-key", OriginalFileName = "new", MimeType = mimeType };

        SetupUserContext("10", "testName", "test@test.com");
        _mockFileService.GetFileByIdAsync(50).Returns(oldFile);
        _mockFileService.GetFileByIdAsync(101).Returns(newFile);
        _mockFileService.SaveFileAsync(mockfile, 10).Returns(newFile);

        return mockfile;
    }

    [Fact]
    public async Task UploadProfilePicture_ValidImage_ReplacesOldAndReturnsOk()
    {
        var mockfile = await CreateMockFile([0x89, 0x50, 0x4e, 0x47], "image/png");

        var result = await _controller.UploadProfilePicture(mockfile, "0", "0", "1");
        result.Result.ShouldBeOfType<OkResult>();

        await _mockFileService.Received(1).DeleteFileAsync("old-key");
        
        await using var verifyContext = DbContextFactory.CreateDbContext();
        var updatedUser = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == 10);
        updatedUser.ShouldNotBeNull();
        updatedUser.ProfilePicture.ShouldBe(101);
        updatedUser.ProfilePictureOffsetX.ShouldBe(0f);
        updatedUser.ProfilePictureOffsetY.ShouldBe(0f);
        updatedUser.ProfilePictureZoom.ShouldBe(1f);
    }

    [Fact]
    public async Task UploadProfilePicture_ImageInvalidRealMime_UnsupportedType()
    {
        await SetupTestUser();
        var mockfile = await CreateMockFile([0x89, 0x50, 0x4e, 0xff], "image/png");

        var result = await _controller.UploadProfilePicture(mockfile, "0", "0", "1");
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result.Result).Value.ShouldBe("Image not added, supported file types are .jpeg, .png, .svg, .gif, .webp");
    }

    [Fact]
    public async Task UploadProfilePicture_ImageTooBig_Fail()
    {
        var stream = new MemoryStream([0x89, 0x50, 0x4e, 0x47]);
        var mockfile = Substitute.For<IFormFile>();
        mockfile.ContentType.Returns("image/png");
        mockfile.Length.Returns(10000001);
        mockfile.OpenReadStream().Returns(stream);
        
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User{ Email = "test@test.com", Country = "NZ", DisplayName = "testName", Id = 10, ProfilePicture = 50 });
        await context.SaveChangesAsync();
        var oldFile = new CustomFile { Id = 50, OwnerId = 10, FileKey = "old-key", OriginalFileName = "old", MimeType = "image/png" };
        var newFile = new CustomFile { Id = 101, OwnerId = 10, FileKey = "new-key", OriginalFileName = "new", MimeType = "image/png" };

        SetupUserContext("10", "testName", "test@test.com");
        _mockFileService.GetFileByIdAsync(50).Returns(oldFile);
        _mockFileService.GetFileByIdAsync(101).Returns(newFile);
        _mockFileService.SaveFileAsync(mockfile, 10).Returns(newFile);

        var result = await _controller.UploadProfilePicture(mockfile, "0", "0", "1");
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result.Result).Value.ShouldBe("Image not added, image is too large, maximum file size is 10MB");
    }
}