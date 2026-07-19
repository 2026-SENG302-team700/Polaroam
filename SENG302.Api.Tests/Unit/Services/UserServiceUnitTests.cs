using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using Shouldly;
using NSubstitute;
using Microsoft.Extensions.Time.Testing;

namespace SENG302.Api.Tests.Unit.Services;

public class UserServiceUnitTest
{
    private readonly IUserService _userService;
    private readonly IDbContextFactory<DatabaseContext> _mockDbContextFactory;
    private readonly DatabaseContext _mockDatabaseContext;
    private readonly FakeTimeProvider _fakeTimeProvider;
    
    public UserServiceUnitTest()
    {
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _mockDbContextFactory = Substitute.For<IDbContextFactory<DatabaseContext>>();
        _mockDatabaseContext = Substitute.For<DatabaseContext>();

        _mockDbContextFactory.CreateDbContextAsync().Returns(_mockDatabaseContext);

        _userService = new UserService(_mockDbContextFactory, _fakeTimeProvider);
    }

    [Fact]
    public async Task CheckUserCredentials_UserDoesNotExist_ExpectDoesNotExistResult()
    {
        var result = await _userService.CheckUserCredentialsAsync("nonexistent@test.com", "AnyPassword1!");
        
        Assert.Equal(UserVerificationResult.DoesNotExist, result.userVerificationResult);
        Assert.Null(result.user);
    }
    
    [Fact]
    public async Task CheckUserCredentials_UnverifiedEmail_ExpectAccountUnverifiedResult()
    {
        var email = "unverified@test.com";
        
        var user = await _userService.GenerateNewUserAsync(email, "Test User", "Password123!", "NZ");
        user.EmailVerified = false;
        user.CodeGenerationTime = _fakeTimeProvider.GetUtcNow().ToUnixTimeSeconds() - 20;
        
        _mockDatabaseContext.Users.FirstOrDefaultAsync().ReturnsForAnyArgs(user);

        var result = await _userService.CheckUserCredentialsAsync(email, "Password123!");
        Assert.Equal(UserVerificationResult.AccountUnverified, result.userVerificationResult);
        Assert.NotNull(result.user);
    }
    
    // [Fact]
    // public async Task CheckUserCredentials_UnverifiedEmailTimeExpired_ExpectAccountUnverifiedResult()
    // {
    //     var email = "unverified@test.com";
    //     using (var context = await DbFactory.CreateDbContextAsync())
    //     {
    //         var user = await UserServiceUnderTest.GenerateNewUserAsync(email, "Test User", "Password123!", "NZ");
    //         user.EmailVerified = false;
    //         user.CodeGenerationTime = _fakeTimeProvider.GetUtcNow().ToUnixTimeSeconds() - 400;
    //         context.Users.Add(user);
    //         await context.SaveChangesAsync();
    //     }
    //     var result = await UserServiceUnderTest.CheckUserCredentialsAsync(email, "Password123!");
    //     Assert.Equal(UserVerificationResult.DoesNotExist, result.userVerificationResult);
    //     Assert.NotNull(result.user);
    // }
    
    // [Fact]
    // public async Task UpdateUser_ValidRequest_ExpectUpdatedUser()
    // {
    //     int userId;
    //     using (var context = await DbFactory.CreateDbContextAsync())
    //     {
    //         var user = new User { Email = "old@test.com", DisplayName = "Old Name", Country = "NZ", PasswordKey = "hash" };
    //         context.Users.Add(user);
    //         await context.SaveChangesAsync();
    //         userId = user.Id;
    //     }

    //     var updatedUser = await UserServiceUnderTest.UpdateUser(userId, "new@test.com", "New Name", "AU", true);

    //     Assert.NotNull(updatedUser);
    //     Assert.Equal("new@test.com", updatedUser.Email);
    //     Assert.Equal("New Name", updatedUser.DisplayName);
    //     Assert.Equal("AU", updatedUser.Country);
    //     Assert.True(updatedUser.ProfanityFiltering);
    // }

    [Fact]
    public void ValidateProfanityFilterDisabled_NewAccount_ExpectProfanityFilterFalse()
    {
        var user = new User
        {
            Email = "hello@example.com",
            DisplayName = "Hello",
            Country = "NZ",
        };
        
        Assert.False(user.ProfanityFiltering);
    }
}