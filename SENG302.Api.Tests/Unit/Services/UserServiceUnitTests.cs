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
using Microsoft.EntityFrameworkCore.Storage;

namespace SENG302.Api.Tests.Unit.Services;

public class UserServiceUnitTest : DatabaseUnitTestFixture
{
    private readonly IUserService _userService;

    private readonly FakeTimeProvider _fakeTimeProvider;
    
    public UserServiceUnitTest()
    {
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

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
        user.CodeGenerationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 20;

        using (var mockDatabaseContext = await _mockDbContextFactory.CreateDbContextAsync()) {
            mockDatabaseContext.Users.Add(user);
            await mockDatabaseContext.SaveChangesAsync();
        }

        var result = await _userService.CheckUserCredentialsAsync(email, "Password123!");
        Assert.Equal(UserVerificationResult.AccountUnverified, result.userVerificationResult);
        Assert.NotNull(result.user);

    }
    
    [Fact]
    public async Task CheckUserCredentials_UnverifiedEmailTimeExpired_ExpectAccountUnverifiedResult()
    {
        var email = "unverified@test.com";
        var user = await _userService.GenerateNewUserAsync(email, "Test User", "Password123!", "NZ");
        user.EmailVerified = false;
        user.CodeGenerationTime = _fakeTimeProvider.GetUtcNow().ToUnixTimeSeconds() - 400;

        using (var mockDatabaseContext = await _mockDbContextFactory.CreateDbContextAsync()) {
            mockDatabaseContext.Users.Add(user);
            await mockDatabaseContext.SaveChangesAsync();
        }
        
        var result = await _userService.CheckUserCredentialsAsync(email, "Password123!");
        Assert.Equal(UserVerificationResult.DoesNotExist, result.userVerificationResult);
        Assert.NotNull(result.user);
    }
    
    [Fact]
    public async Task UpdateUser_ValidRequest_ExpectUpdatedUser()
    {
        int userId;
        
        using (var mockDatabaseContext = await _mockDbContextFactory.CreateDbContextAsync()) {
            var user = new User { Email = "old@test.com", DisplayName = "Old Name", Country = "NZ", PasswordKey = "hash" };
            mockDatabaseContext.Users.Add(user);
            await mockDatabaseContext.SaveChangesAsync();
            userId = user.Id;
        }

        var updatedUser = await _userService.UpdateUser(userId, "new@test.com", "New Name", "AU", true);

        Assert.NotNull(updatedUser);
        Assert.Equal("new@test.com", updatedUser.Email);
        Assert.Equal("New Name", updatedUser.DisplayName);
        Assert.Equal("AU", updatedUser.Country);
        Assert.True(updatedUser.ProfanityFiltering);
    }

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