using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Jobs;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services.ImageTagging;
using SENG302.Api.Tests.Acceptance.Setup;
using static SENG302.Api.Helpers.DateHelper;
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
    
    private async Task SetupUserTripAndPost()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country",
            AcceptedAiPrivacyPolicy = true,
            AiTaggingEnabled = true
        });

        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "Original trip",
            Description = "Original description",
            StartDate = DateToDateTimeOffset(2026, 7, 1),
            EndDate = DateToDateTimeOffset(2026, 7, 10),
            UserId = 1
        });

        context.Posts.Add(new Post
        {
            PostId = 1,
            TripId = 1,
            Title = "My post",
            Content = "",
            PhotoIds = [],
            OsmId = 1,
            LocationName = "Christchurch"
        });
        
        context.Albums.Add(new Album { TripId = 1, PhotoIds = new() });

        await context.SaveChangesAsync();
    }
    
    private void SetupGeoMock()
    {
        _fixture.GeoServiceMock.GetLocationAsync(Arg.Any<long>()).Returns(new GeoLocationSearchResult
        {
            OsmId = 1,
            Latitude = "1",
            Longitude = "1",
            DisplayName = "Auckland",
            Name = "Auckland, New Zealand"
        });
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


    [Given(@"an image has been uploaded to a post or album And the queue for tagging is full")]
    public async Task GivenAnImageHasBeenUploaded()
    {
        await SetupUserTripAndPost();
        
        using var content = new MultipartFormDataContent();

        string _fileName = "";
        byte[] _fileBytes = [];
        string _contentType = "";
        
        _fileName = "panda.webp";
        _fileBytes = await File.ReadAllBytesAsync("resources/panda.webp");;
        _contentType = "image/webp";

        
        var fileContent = new ByteArrayContent(_fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(_contentType);
        content.Add(fileContent, "files", _fileName);

        content.Add(new StringContent("1"), "Id");
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("My post"), "Title");
        content.Add(new StringContent("dsfdsfd"), "Content");
        content.Add(new StringContent("1"), "osmId");
        content.Add(new StringContent(DateTimeOffset.UtcNow.ToString("s") + "Z"), "Date");
        
        SetupGeoMock();
        
        _fixture.ImageTaggingQueueMock.QueueImageTaggingAsync(Arg.Any<ImageTaggingJob>()).Returns(false);
        
        _lastResponse = await _fixture.HttpClient.PostAsync("api/post", content);
        var cont = await _lastResponse.Content.ReadAsStringAsync();
    }

    [When(@"I view the image in enlarged view")]
    public async Task WhenIViewTheImageInEnlargedView()
    {
        _lastResponse = await _fixture.HttpClient.GetAsync("/api/image/1/tags");
    }


    [Then(
        @"I see an icon with tool-tip letting me know the image has been tagged with AI And I can click a button to retry tagging")]
    public async Task ThenICanClickAButtonToRetryTagging()
    {
        _lastResponse.ShouldNotBeNull();
        var content = await _lastResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        content.Detail.ShouldBe("Queue was full");
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