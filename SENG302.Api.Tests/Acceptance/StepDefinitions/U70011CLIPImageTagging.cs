using System.Net;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U70011ClipImageTagging
{
    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;

    public U70011ClipImageTagging(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Given(@"I have just registered for Polaroam")]
    public async Task GivenIHaveJustRegisteredForPolaroam()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "test user",
            PasswordKey = "P4$$word",
            Country = "NZ"
        });
        await context.SaveChangesAsync();
    }

    [When(@"I check my settings")]
    public async Task WhenICheckMySettings()
    {
        _lastResponse = await _fixture.HttpClient.GetAsync("api/user");
    }

    [Then(@"I can see that AI image tagging is disabled")]
    public async Task ThenICanSeeThatAiImageTaggingIsDisabled()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == 1);
        user!.AiTaggingEnabled.ShouldBeFalse();
    }

    [Given(@"I have accepted the AI privacy policy")]
    public async Task GivenIHaveAcceptedTheAiPrivacyPolicy()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "test user",
            PasswordKey = "P4$$word",
            Country = "NZ",
            AcceptedAiPrivacyPolicy = true
        });
        await context.SaveChangesAsync();
    }

    [Given(@"AI image tagging is enabled")]
    public async Task GivenAiImageTaggingIsEnabled()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == 1);
        user!.AiTaggingEnabled = true;
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    [When(@"I toggle AI image tagging on")]
    [When(@"I toggle AI image tagging off")]
    public async Task WhenIToggleAiImageTagging()
    {
        _lastResponse = await _fixture.HttpClient.PutAsync("api/user/ai-toggle", null);
    }

    [Then(@"AI image tagging is enabled")]
    public async Task ThenAiImageTaggingIsEnabled()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == 1);
        user!.AiTaggingEnabled.ShouldBeTrue();
    }

    [Then(@"AI image tagging is disabled")]
    public async Task ThenAiImageTaggingIsDisabled()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == 1);
        user!.AiTaggingEnabled.ShouldBeFalse();
    }
}