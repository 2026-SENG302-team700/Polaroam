using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Shouldly;
using SENG302.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace SENG302.Api.Tests.E2E;

public class LoginE2ETest : E2ETestFixture
{
    public LoginE2ETest(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task ShouldLoginSuccessfully()
    {
        
        // Arrange: Create a test user in the database
        var email = "testuser@example.com";
        var password = "TestPassword123";
        var displayName = "Test User";
        var countryCode = "NZ";

        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        if (!dbContext.Users.Any(u => u.Email.ToLower() == email))
        {

        // Register user via API to ensure password is hashed correctly
            var user = new User
            {
                Email = email,
                DisplayName = displayName,
                Country = countryCode,
                EmailVerified = true
            };
            var hasher = new PasswordHasher<User>();
            user.PasswordKey = hasher.HashPassword(user, password);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }
        Console.WriteLine("\n\nUser Created\n\n");

        // Act: Navigate to login page and fill in the form
        await Page.GotoAsync("http://localhost/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill in the login form
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#password").FillAsync(password);

        // Click the login button
        await Page.Locator("button[type='submit']").ClickAsync();

        // If we wanted to wait for navigation to complete to a specific page...
        await Page.WaitForURLAsync("http://localhost/home/");
        
        // You may instead want to look for elements on the page with something like
        // await Page.Locator("h1:has-text('Books')").WaitForAsync();

        // Assert: Verify we're on the home page (logged in)
        var currentUrl = Page.Url;
        currentUrl.ShouldBe("http://localhost/home/");
    }

    [Fact]
    public async Task ShouldShowErrorWithInvalidCredentials()
    {
        // Act: Navigate to login page
        await Page.GotoAsync("http://localhost/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill in invalid credentials
        await Page.Locator("#email").FillAsync("invalid@example.com");
        await Page.Locator("#password").FillAsync("WrongPassword123");

        // Click the login button
        await Page.Locator("button[type='submit']").ClickAsync();

        // Wait for error message to appear
        await Page.WaitForSelectorAsync(".alert-danger");

        // Assert: Verify error message is displayed
        var errorMessage = await Page.Locator(".alert-danger").TextContentAsync();
        errorMessage.ShouldNotBeNullOrEmpty();
        errorMessage.ShouldContain("Login failed");
    }
}

// Helper record for CSRF token response
file record CsrfTokenResponse(string Token);
