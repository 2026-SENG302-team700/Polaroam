using System.Net;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NSubstitute;
using SENG302.Api.Models.Responses;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U70011ClipImageTagging
{
    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;
    private Post? _post;
    private int _userId = 1;

    public U70011ClipImageTagging(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }
    private async Task SetupTripAndAlbum()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        context.Trips.Add(new Trip
        {
            Id = 1,
            UserId = _userId,
            Title = "AI tagging trip",
            Description = "Trip for testing image tagging",
            StartDate = DateToDateTimeOffset(2026, 1, 1),
            EndDate = DateToDateTimeOffset(2026, 1, 10)
        });

        context.Albums.Add(new Album
        {
            TripId = 1,
            PhotoIds = []
        });

        await context.SaveChangesAsync();
    }

    private async Task UploadPostImage(bool aiTaggingForUpload)
    {
        await SetupTripAndAlbum();

        _fixture.GeoServiceMock
            .GetLocationAsync(Arg.Any<long>())
            .Returns(new GeoLocationSearchResult
            {
                OsmId = 1,
                Latitude = "1",
                Longitude = "1",
                Name = "Christchurch",
                DisplayName = "Christchurch, New Zealand"
            });

        var imageBytes = await File.ReadAllBytesAsync(
            "resources/render.png"
        );

        using var form = new MultipartFormDataContent();

        form.Add(new StringContent("1"), "TripId");
        form.Add(new StringContent("AI tagging post"), "Title");
        form.Add(new StringContent("Post containing an image"), "Content");
        form.Add(new StringContent("1"), "OsmId");
        form.Add(
            new StringContent(
                DateTimeOffset.UtcNow.AddDays(-1).ToString("O")
            ),
            "Date"
        );
        form.Add(
            new StringContent(aiTaggingForUpload.ToString()),
            "aiTaggingForUpload"
        );

        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType =
            new MediaTypeHeaderValue("image/png");

        form.Add(imageContent, "files", "render.png");

        _lastResponse = await _fixture.HttpClient.PostAsync(
            "api/post",
            form
        );

        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        _post = await _lastResponse.Content.ReadFromJsonAsync<Post>();
        _post.ShouldNotBeNull();
        _post.PhotoIds.ShouldHaveSingleItem();
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


    [When("I upload a supported image to a post with AI tagging enabled for this upload")]
    public async Task UploadImageWithTaggingEnabled()
    {
        await UploadPostImage(aiTaggingForUpload: true);
    }

    [When("I upload a supported image to a post with AI tagging disabled for this upload")]
    public async Task UploadImageWithTaggingDisabled()
    {
        await UploadPostImage(aiTaggingForUpload: false);
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

    [Then("the image is queued for AI tagging")]
    public async Task ImageIsQueuedForTagging()
    {
        _post.ShouldNotBeNull();

        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var file = await context.CustomFiles.FindAsync(
            _post.PhotoIds[0]
        );

        file.ShouldNotBeNull();
        file.InTagQueue.ShouldBeTrue();
    }

    [Then("my AI image-tagging account setting remains enabled")]
    public async Task AiTaggingAccountSettingRemainsEnabled()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var user = await context.Users.FindAsync(_userId);

        user.ShouldNotBeNull();
        user.AiTaggingEnabled.ShouldBeTrue();
    }

    [Then("the image is not queued for AI tagging")]
    public async Task ImageIsNotQueuedForTagging()
    {
        _post.ShouldNotBeNull();

        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var file = await context.CustomFiles.FindAsync(
            _post.PhotoIds[0]
        );

        file.ShouldNotBeNull();
        file.InTagQueue.ShouldBeFalse();
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