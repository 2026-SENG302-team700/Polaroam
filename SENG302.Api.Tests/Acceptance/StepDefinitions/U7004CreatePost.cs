using System.Net.Http.Json;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;
using System.Net;
using NSubstitute;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7004CreatePost
{

    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;
    private string _title;
    private string _content;
    private DateTime _date;
    private string _location;

    public U7004CreatePost(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
        _title = "";
        _content = "";
        _date = new DateTime();
        _location = "";

    }
    
    private void SetupGeoMock()
    {
        _fixture.GeoServiceMock.GetLocationAsync(Arg.Any<long>()).Returns(new GeoLocationSearchResult
        {
            osm_id = 1,
            lat = "1",
            lon = "1",
            display_name = "Auckland",
            name = "Auckland, New Zealand"
        });
    }

    [Given(
        @"I am on the create post form And I enter a valid title, location, date, and optional content")]
    public async Task ValidTripAttributes()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "Original trip",
            Description = "Original description",
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 10),
            UserId = 1
        });
        await context.SaveChangesAsync();

        _title = "Test Post";
        _content = "Test content";
        _location = "Test Locatiom";
        _date = DateTime.Parse("2028-01-07");


    }

    [When(@"I click 'Create Post'")]
    public async Task CreatePost()
    {
        NewPostRequest postRequest = new NewPostRequest()
        {
            TripId = 1,
            Title = _title,
            Content = _content,
            OsmId = 292806332,
            Date = _date
        };
        
        SetupGeoMock();
        _lastResponse = await _fixture.HttpClient.PostAsJsonAsync("api/post", postRequest);
    }

    [Then(@"the post is persisted")]
    public async Task CheckPostIsPersisted()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var post = await _lastResponse.Content.ReadFromJsonAsync<Post>();

        var retrievedPost = await _fixture.HttpClient.GetFromJsonAsync<Post>($"api/post/{post.PostId}");

        retrievedPost.ShouldNotBeNull();
        retrievedPost.PostId.ShouldBe(post.PostId);
    }
}