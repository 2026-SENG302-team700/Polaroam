using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7005ImageValidation
{
    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;
    private string _fileName = "";
    private byte[] _fileBytes = [];
    private string _contentType = "";

    public U7005ImageValidation(AcceptanceTestFixture fixture)
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
            OsmType = "node",
            LocationName = "Christchurch"
        });

        await context.SaveChangesAsync();
    }

    [Given(@"I have selected some files in the file selector And at least one is larger than 10 MB")]
    public async Task GivenAFileLargerThan10MB()
    {
        await SetupUserTripAndPost();

        _fileName = "huge.png";
        _fileBytes = new byte[10_000_001];
        _contentType = "image/png";
    }

    [Given(@"I have selected some files in the file selector And at least one is not of the type jpeg, png, svg, webp, or gif")]
    public async Task GivenAFileOfAnUnsupportedType()
    {
        await SetupUserTripAndPost();

        _fileName = "notes.txt";
        _fileBytes = Encoding.UTF8.GetBytes("this is not an image");
        _contentType = "text/plain";
    }

    [When(@"I click 'Select'")]
    public async Task WhenIClickSelect()
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(_fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(_contentType);
        content.Add(fileContent, "files", _fileName);

        content.Add(new StringContent("1"), "Id");
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("My post"), "Title");
        content.Add(new StringContent(""), "Content");
        content.Add(new StringContent("1"), "osmId");
        content.Add(new StringContent("node"), "osmType");
        content.Add(new StringContent(DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")), "Date");

        _lastResponse = await _fixture.HttpClient.PutAsync("api/post/1", content);
    }

    [Then(@"I see an image validation error message '(.*)'")]
    public async Task ThenISeeAnImageValidationErrorMessage(string expectedMessage)
    {
        _lastResponse.ShouldNotBeNull();
        _lastResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errors = await _lastResponse.Content.ReadFromJsonAsync<BadRequestValidationListResponse>();
        errors.ShouldNotBeNull();

        var expectedMessageBody = expectedMessage.Replace("{filename} ", "");
        errors.Errors["photos"].ShouldContain(e => e.Contains(_fileName) && e.Contains(expectedMessageBody.Split(',')[0]));
    }
}