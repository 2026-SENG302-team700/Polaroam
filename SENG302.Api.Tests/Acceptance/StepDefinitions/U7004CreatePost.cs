using System.Net.Http.Json;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;
using System.Net;
using NSubstitute;
using SENG302.Api.Models.Responses;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7004CreatePost
{

    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;
    private string _title;
    private string _content;
    private DateTimeOffset? _date;
    private string _location;
    private long _osmId;

    public U7004CreatePost(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
        _title = "";
        _content = "";
        _date = new DateTimeOffset();
        _location = "";
        _osmId = 292806332;
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

    public async Task SetupUserAndTrip()
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
            StartDate = DateToDateTimeOffset(2026, 7, 1),
            EndDate = DateToDateTimeOffset(2026, 7, 10),
            UserId = 1
        });
        await context.SaveChangesAsync();
    }

    [Given(
        @"I am on the create post form And I enter a valid title, location, date, and optional content")]
    public async Task ValidTripAttributes()
    {
        await SetupUserAndTrip();

        _title = "Test Post";
        _content = "Test content";
        _location = "Test Locatiom";
        _date = DateToDateTimeOffset(2025, 1, 7);


    }

    [Given(
      @"I am on the create post form And I enter a title that is not between 3 and 128 characters")]
    public async Task GivenIEnterAnInvalidPostTitle()
    {
        await SetupUserAndTrip();

        _title = "ab";
        _content = "Test content";
        _date = DateToDateTimeOffset(2022, 1, 7);
    }

    [Given(@"I enter location text without selecting a suggested location")]
    public async Task GivenIEnterLocationTextWithoutSelectingLocation()
    {
        await SetupUserAndTrip();

        _title = "Valid post title";
        _content = "Valid post content";
        _date = DateTimeOffset.UtcNow.AddDays(-1);

        // Text was entered, but no search result was selected.
        _location = "Made up location";
        _osmId = 0;
    }

    [When(@"I click 'Create Post'")]
    public async Task CreatePost()
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(_title), "Title");
        form.Add(new StringContent(_content), "Content");
        form.Add(new StringContent(_osmId.ToString()), "OsmId");
        form.Add(new StringContent("1"), "TripId");
        form.Add(new StringContent(_date == null ? string.Empty : (_date?.ToString("s") + "Z")), "Date");

        SetupGeoMock();
        _lastResponse = await _fixture.HttpClient.PostAsync("api/post", form);
    }

    [Then(@"the post is persisted")]
    public async Task CheckPostIsPersisted()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var post = await _lastResponse.Content.ReadFromJsonAsync<Post>();

        post.ShouldNotBeNull();

        var retrievedPost = await _fixture.HttpClient.GetFromJsonAsync<Post>($"api/post/{post.PostId}");

        retrievedPost.ShouldNotBeNull();
        retrievedPost.PostId.ShouldBe(post.PostId);
    }

    // AC 4
    [Given(@"I am on the create post form And I enter a miss-formed or null date")]
    public async Task SetInvalidDate()
    {
        await SetupUserAndTrip();

        _title = "Valid post title";
        _content = "Valid post content";
        _osmId = 292806332;
        _date = null;
    }

    // AC 5
    [Given(@"I am on the create post form And I enter a date in the future")]
    public async Task SetFutureDate()
    {
        await SetupUserAndTrip();

        _title = "Valid post title";
        _content = "Valid post content";
        _date = DateTimeOffset.UtcNow.AddDays(1);

    }

    [Given(@"I enter content longer than 8196 characters long")]
    public async Task GivenIEnterContentLongerThan8196Characters()
    {
        await SetupUserAndTrip();

        _title = "Valid post title";
        _content = new string('a', 8197);
        _date = DateTimeOffset.UtcNow.AddDays(-1);
    }

    [Then(@"I see an error message '(.*)'")]
    public async Task CheckFutureDateErrorMessage(string expectedMessage)
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var error = await _lastResponse.Content.ReadAsStringAsync();
        error.ShouldContain(expectedMessage);
    }
}
