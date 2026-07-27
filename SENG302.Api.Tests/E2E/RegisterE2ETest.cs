using Microsoft.EntityFrameworkCore;

namespace SENG302.Api.Tests.E2E;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Microsoft.Playwright;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using Shouldly;


public class LoginE2ETest : E2ETestFixture
{
    public LoginE2ETest(WebApplicationFactory<SENG302.Api.Program> factory) : base(factory)
    {
    }


    [Fact]
    public async Task ShouldRegisterSuccessfully()
    {
        // Arrange: Create a test user in the database
        var email = "testuser@example.com";
        var password = "TestPassword123!";
        var displayName = "Test User";


        // Act: Navigate to login page and fill in the form
        await Page.GotoAsync($"{AppBaseUrl}/register", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 10000 });

        // Fill in the register form
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#display-name-input").FillAsync(displayName);
        await Page.Locator("#initial-password-input").FillAsync(password);
        await Page.Locator("#confirm-password").FillAsync(password);

        await Page.Locator("#country").ClickAsync();
        await Page.Locator("#country").SelectOptionAsync("NZ");

        // Click the regsiter button
        await Page.Locator("button[type='submit']").ClickAsync();

        await Page.WaitForTimeoutAsync(1000);

        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Assert: Verify we're on the verify code page
        var currentUrl = Page.Url;
        currentUrl.ShouldBe($"{AppBaseUrl}/register/verification");
    }

    [Fact]
    public async Task ShouldShowErrorWithInvalidPassword()
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();


        // Arrange: Create a test user in the database
        var email = "testuser@example.com";
        var password = "Test";
        var displayName = "Test User";


        // Act: Navigate to login page and fill in the form
        await Page.GotoAsync($"{AppBaseUrl}/register", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 10000 });

        // Fill in the login form
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#display-name-input").FillAsync(displayName);
        await Page.Locator("#initial-password-input").FillAsync(password);
        await Page.Locator("#confirm-password").FillAsync(password);

        await Page.Locator("#country").ClickAsync();

        await Page.Locator("#country").SelectOptionAsync("NZ");

        // Click the login button
        await Page.Locator("button[type='submit']").ClickAsync();

        await Page.WaitForTimeoutAsync(1000);

        // Wait for error message to appear
        await Page.WaitForSelectorAsync("#initial-password-error");

        // Assert: Verify error message is displayed
        var errorMessage = await Page.Locator("#initial-password-error").TextContentAsync();
        errorMessage.ShouldNotBeNullOrEmpty();
        errorMessage.ShouldContain("Password must be at least 8 characters long including at least one of each uppercase, lowercase, numbers and special characters");
    }
}

// Helper record for CSRF token response
file record CsrfTokenResponse(string Token);
