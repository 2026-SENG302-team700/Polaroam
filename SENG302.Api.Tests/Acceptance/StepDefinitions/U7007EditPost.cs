using System.Net.Http.Json;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;
using System.Net;
using NSubstitute;
using System.Net.Http.Headers;
using SENG302.Api.Models.Responses;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7007EditPost
{

    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;
    private string _title;
    private string _content;
    private DateTime? _date;
    private string _location;
    private long _osmId;

    public U7007EditPost(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
        _title = "";
        _content = "";
        _date = new DateTime();
        _location = "";
        _osmId = 1;
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

    public async Task SetupUserTripAndPost(bool setUpGeo)
    {
        if (setUpGeo)
        {
            SetupGeoMock();
        }
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
        await context.SaveChangesAsync();
    }
    
    private MultipartFormDataContent BuildUpdatePostForm(
        long osmId = 1,
        IEnumerable<(byte[] Bytes, string FileName, string ContentType)>? photos = null)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "Id");
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent(_title), "Title");
        content.Add(new StringContent(_content), "Content");
        content.Add(new StringContent(_osmId.ToString()), "OsmId");
        content.Add(new StringContent(_date?.ToString("o") ?? ""), "Date");

        foreach (var (bytes, fileName, contentType) in photos ?? [])
        {
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "files", fileName);
        }

        return content;
    }

    [Given(
        @"I am on the edit post form page And I enter a valid title, location, date, images and content")]
    public async Task ValidTripAttributes()
    {
        await SetupUserTripAndPost(true);

        _title = "Updated title";
        _content = "Updated content";
        _date = DateTime.Parse("2025-01-07");
    }

    [Given(
      "I am editing an existing post and enter location text without selecting a suggested location"
  )]
    public async Task GivenIEnterInvalidLocationWhileEditingPost()
    {
        await SetupUserTripAndPost(false);

        _title = "Valid updated title";
        _content = "Valid updated content";
        _date = DateTime.Today.AddDays(-1);
        
        // Text was entered without selecting a suggested location.
        _location = "Made up location";
        _osmId = 0;
    }

    [Then("I see the edit post error message {string}")]
    public async Task ThenISeeTheEditPostErrorMessage(string expectedMessage)
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var responseBody = await _lastResponse.Content.ReadAsStringAsync();
        Console.WriteLine(responseBody);
        responseBody.ShouldContain(expectedMessage);
    }

    [When(@"I click 'Update Post'")]
    public async Task UpdatePost()
    {
        
        var pandaBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        var pngBytes = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

        using var updateRequest = BuildUpdatePostForm();
        _lastResponse = await _fixture.HttpClient.PutAsync("api/post/1", updateRequest);
    }

    [Then(@"the post is updated")]
    public async Task CheckPostIsPersisted()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var post = await _lastResponse.Content.ReadFromJsonAsync<Post>();

        post.Title.ShouldBe(_title);
        post.Content.ShouldBe(_content);
        post.Date.ShouldBe(_date);
    }

    [Then("the post is not updated")]
    public async Task ThenThePostIsNotUpdated()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var post = await context.Posts.FindAsync(1);

        post.ShouldNotBeNull();
        post.Title.ShouldBe("My post");
        post.Content.ShouldBe("");
        post.OsmId.ShouldBe(1);
        post.LocationName.ShouldBe("Christchurch");
    }
}