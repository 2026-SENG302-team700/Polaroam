using System.Net.Http.Json;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;
using System.Net;
using NSubstitute;
using System.Net.Http.Headers;
using SENG302.Api.Models.Responses;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Requests;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7007EditPost
{

    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;
    private string _title;
    private string _content;
    private DateOnly? _date;
    private string _location;
    private long _osmId;

    private readonly List<(byte[] Bytes, string FileName, string ContentType)> _images = [];

    public U7007EditPost(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
        _title = "";
        _content = "";
        _date = new DateOnly();
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

    public async Task SetupUserTripAndPost(bool setUpGeo = true)
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
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 10),
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
        content.Add(new StringContent(_date?.ToString("yyyy-MM-dd") ?? ""), "Date");

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
        _date = DateOnly.Parse("2025-01-07");
    }

    [Given("I am editing an existing post with two valid images to add")]
    public async Task GivenIAmEditingPostWithTwoValidImages()
    {
        await SetupUserTripAndPost();

        _title = "Updated title";
        _content = "Updated content";
        _date = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
        _osmId = 1;

        var pandaBytes =
            await File.ReadAllBytesAsync("resources/panda.webp");

        _images.Add((
            pandaBytes,
            "panda.webp",
            "image/webp"
        ));

        _images.Add((
            pandaBytes,
            "second-panda.webp",
            "image/webp"
        ));
    }

    [Given("I am editing an existing post with an image larger than 10MB")]
    public async Task GivenIAmEditingPostWithOversizedImage()
    {
        await SetupUserTripAndPost();

        _title = "Updated title";
        _content = "Updated content";
        _date = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
        _osmId = 1;

        _images.Add((
            new byte[10_000_001],
            "huge.png",
            "image/png"
        ));
    }

    [Given("I am editing an existing post with an unsupported image type")]
    public async Task GivenIAmEditingPostWithInvalidImageType()
    {
        await SetupUserTripAndPost();

        _title = "Updated title";
        _content = "Updated content";
        _date = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
        _osmId = 1;

        _images.Add((
            Encoding.UTF8.GetBytes("This is not an image"),
            "notes.txt",
            "text/plain"
        ));
    }

    [Given(
      "I am editing an existing post and enter location text without selecting a suggested location"
  )]
    public async Task GivenIEnterInvalidLocationWhileEditingPost()
    {
        await SetupUserTripAndPost(false);

        _title = "Valid updated title";
        _content = "Valid updated content";
        _date = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);

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
        using var form = new MultipartFormDataContent();

        form.Add(new StringContent("1"), "Id");
        form.Add(new StringContent("1"), "TripId");
        form.Add(new StringContent(_title), "Title");
        form.Add(new StringContent(_content), "Content");
        form.Add(new StringContent(_osmId.ToString()), "OsmId");

        if (_date != null)
        {
            form.Add(
                new StringContent(_date.Value.ToString("yyyy-MM-dd")),
                "Date");
        }

        foreach (var image in _images)
        {
            var imageContent = new ByteArrayContent(image.Bytes);

            imageContent.Headers.ContentType =
                new MediaTypeHeaderValue(image.ContentType);

            form.Add(
                imageContent,
                "files",
                image.FileName);
        }

        _lastResponse = await _fixture.HttpClient.PutAsync(
            "api/post/1",
            form);
    }

    [Then(@"the post is updated")]
    public async Task CheckPostIsPersisted()
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var post = await _lastResponse.Content.ReadFromJsonAsync<Post>();

        post.ShouldNotBeNull();
        post.Title.ShouldBe(_title);
        post.Content.ShouldBe(_content);
    }

    [Then("the two new images are associated with the post")]
    public async Task ThenTwoNewImagesAreAssociatedWithPost()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var post = await context.Posts
            .SingleAsync(post => post.PostId == 1);

        post.PhotoIds.ShouldNotBeNull();
        post.PhotoIds.Count.ShouldBe(2);

        var files = await context.CustomFiles
            .Where(file => post.PhotoIds.Contains(file.Id))
            .ToListAsync();

        files.Count.ShouldBe(2);
        files.Select(file => file.OriginalFileName)
            .ShouldBe(["panda.webp", "second-panda.webp"], ignoreOrder: true);
    }

    [Then("no new images are associated with the post")]
    public async Task ThenNoNewImagesAreAssociatedWithPost()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var post = await context.Posts
            .SingleAsync(post => post.PostId == 1);

        post.PhotoIds.ShouldNotBeNull();
        post.PhotoIds.ShouldBeEmpty();

        var storedFiles = await context.CustomFiles.CountAsync();
        storedFiles.ShouldBe(0);
    }

    [Then("I see the edit post photo error message {string}")]
    public async Task ThenISeeTheEditPostPhotoErrorMessage(
      string expectedMessage)
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var response = await _lastResponse.Content
            .ReadFromJsonAsync<BadRequestValidationListResponse>();

        response.ShouldNotBeNull();
        response.Errors.ShouldContainKey("photos");

        response.Errors["photos"].ShouldContain(
            error => error.Contains(expectedMessage));
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
