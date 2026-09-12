using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Requests;
using SENG302.Api.Models.Entities;
using Shouldly;
using SENG302.Api.Models.Responses;
using System.Net.Http.Headers;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
using static SENG302.Api.Helpers.DateHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Controllers;
using SENG302.Api.Services;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Security.Claims;
using NSubstitute.ExceptionExtensions;
using Xunit.Sdk;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class TripControllerTests : BaseIntegrationTestFixture
{
    private ITestOutputHelper _output;

    private ITripService _mockTripService;
    private IPostService _mockPostService;
    private IAlbumService _mockAlbumService;
    private IUserService _mockUserService;
    private ICollaborationService _mockCollaborationService;

    private TripController _controller;

    public TripControllerTests(WebApplicationFactory<Program> webApplicationFactory, ITestOutputHelper output) :
        base(webApplicationFactory)
    {
        _mockTripService = Substitute.For<ITripService>();
        _mockPostService = Substitute.For<IPostService>();
        _mockAlbumService = Substitute.For<IAlbumService>();
        _mockUserService = Substitute.For<IUserService>();
        _mockCollaborationService = Substitute.For<ICollaborationService>();

        _controller = new TripController(
            _mockTripService,
            _mockPostService,
            _mockAlbumService,
            _mockUserService,
            _mockCollaborationService
        );

        AddTestPrincipalToController(_controller);

        _output = output;
    }

    /// <summary>
    /// Creates and adds to the database a test user for these tests
    /// </summary>
    private async Task AddTestUser()
    {

        User testUser = new()
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        };
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(testUser);
        await context.SaveChangesAsync();

        _mockUserService.GetUserByIdAsync(testUser.Id).Returns(Task.FromResult((User?)testUser));
    }

    /// <summary>
    /// Some trip data that is used by several tests
    /// </summary>
    private readonly NewTripRequest tripTestData = new()
    {
        Title = "Test Trip",
        Description = "Test",
        StartDate = DateToDateTimeOffset(2025, 11, 21),
        EndDate = DateToDateTimeOffset(2025, 12, 21)
    };

    private void SetupGeoMock()
    {
        GeoServiceMock.GetLocationAsync(1).Returns(new GeoLocationSearchResult
        {
            OsmId = 1,
            Latitude = "1",
            Longitude = "1",
            DisplayName = "Auckland",
            Name = "Auckland, New Zealand"
        });
    }


    [Fact]
    public async Task CreateTrip_SuccessfulCreation_ReturnOk()
    {
        await AddTestUser();

        var message = await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTrip_ShortTitle_ReturnBadRequest()
    {
        await AddTestUser();

        var data = new NewTripRequest
        {
            Title = "T",
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Trip title is required and must be between 3 and 128 characters long");
    }


    [Fact]
    public async Task CreateTrip_NonExistentUser_ReturnBadRequest()
    {
        // Test automatically gets userEmail as test@example.com so we don't add the user
        // to the db for this test
        var response = await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("User with the provided id does not exist.");
    }

    [Fact]
    public async Task CreateTrip_InvalidCharactersInName_ReturnBadRequest()
    {
        await AddTestUser();

        var data = new NewTripRequest
        {
            Title = "Trip!",
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Trip title cannot contain characters other than letters, spaces, hyphens, apostrophes, or numbers");
    }

    [Fact]
    public async Task CreateTrip_EndDateBeforeStartDate_ReturnBadRequest()
    {
        await AddTestUser();

        var data = new NewTripRequest
        {
            Title = "Trip!",
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 10, 21)
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("End date must not be before the start date");
    }

    [Fact]
    public async Task CreateTrip_DatesAreNull_ReturnBadRequest()
    {
        await AddTestUser();

        var data = new NewTripRequest
        {
            Title = "Trip!",
            Description = "Test",
            StartDate = null,
            EndDate = null
        };
        var response = await HttpClient.PostAsJsonAsync("/api/trip", data);
        var message = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        message.ShouldContain("Date is invalid");
    }


    [Fact]
    public async Task FetchTripsByUser_SuccessfulFetch_ReturnsTrips()
    {
        await AddTestUser();
        await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);

        // Fetch trips for the user
        var response = await HttpClient.GetAsync("/api/trip");
        response.StatusCode.ShouldBe(HttpStatusCode.OK); // Ensure fetching trips was successful
        Trip[] trips = await response.Content.ReadFromJsonAsync<Trip[]>() ?? Array.Empty<Trip>();
        trips.Length.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteTrip_TripExistsNoPost_Success()
    {
        await AddTestUser();
        var postResponse = await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);
        var trip = await postResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(trip);

        var deleteResponse = await HttpClient.DeleteAsync($"/api/trip/{trip.Id}");
        var responseMessage = await deleteResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        responseMessage.ShouldBe("");
    }

    private MultipartFormDataContent BuildPostForm(
        long osmId = 1,
        IEnumerable<(byte[] Bytes, string FileName, string ContentType)>? photos = null)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("Valid title"), "Title");
        content.Add(new StringContent("Valid content"), "Content");
        content.Add(new StringContent(osmId.ToString()), "osmId");
        content.Add(new StringContent(DateTimeOffset.UtcNow.ToString("s") + "Z"), "Date");

        foreach (var (bytes, fileName, contentType) in photos ?? [])
        {
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "files", fileName);
        }

        return content;
    }

    [Fact]
    public async Task DeleteTrip_TripExistsTripPostExists_Success()
    {
        await AddTestUser();
        var postResponse = await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);
        var trip = await postResponse.Content.ReadFromJsonAsync<Trip>();

        SetupGeoMock();

        var invalidBytesOne = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var invalidBytesTwo = new byte[] { 0xde, 0xad, 0xbe, 0xef };

        using var content = BuildPostForm(photos:
        [
            (invalidBytesOne, "bad1.png", "image/png"),
            (invalidBytesTwo, "bad2.jpeg", "image/jpeg")
        ]);

        await HttpClient.PostAsync("/api/post", content);

        Assert.NotNull(trip);

        var deleteResponse = await HttpClient.DeleteAsync($"/api/trip/{trip.Id}");
        var responseMessage = await deleteResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        responseMessage.ShouldBe("");
    }

    [Fact]
    public async Task DeleteTrip_TripNotExist_BadRequest()
    {
        var deleteResponse = await HttpClient.DeleteAsync($"/api/trip/{0}");
        var responseMessage = await deleteResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
        responseMessage.ShouldBe("Trip does not exist");
    }

    [Fact]
    public async Task DeleteTrip_DifferentUser_Unauthorized()
    {
        var tripId = 10;

        await using var context = DbContextFactory.CreateDbContext();
        context.Trips.Add(new Trip
        {
            Id = tripId,
            Description = tripTestData.Description,
            Title = tripTestData.Title,
            EndDate = tripTestData.EndDate,
            StartDate = tripTestData.StartDate,
            UserId = 100
        });
        await context.SaveChangesAsync();

        var deleteResponse = await HttpClient.DeleteAsync($"/api/trip/{tripId}");

        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateTrip_ExistingTrip_ReturnsOkAndPersistsChanges()
    {
        await AddTestUser();

        var createResponse =
            await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);

        var createdTrip =
            await createResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(createdTrip);

        var newStartDate = DateToDateTimeOffset(2026, 8, 1);
        var newEndDate = DateToDateTimeOffset(2026, 8, 10);

        var request = new UpdateTripRequest
        {
            Id = createdTrip.Id,
            Title = "Updated trip",
            Description = "Updated description",
            StartDate = newStartDate,
            EndDate = newEndDate
        };

        var updateResponse = await HttpClient.PutAsJsonAsync(
            $"/api/trip/{createdTrip.Id}",
            request
        );

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var returnedTrip =
            await updateResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(returnedTrip);
        returnedTrip.Title.ShouldBe("Updated trip");
        returnedTrip.Description.ShouldBe("Updated description");
        returnedTrip.StartDate.ShouldBe(newStartDate);
        returnedTrip.EndDate.ShouldBe(newEndDate);

        var getResponse =
            await HttpClient.GetAsync($"/api/trip/{createdTrip.Id}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var savedTrip =
            await getResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(savedTrip);
        savedTrip.Title.ShouldBe("Updated trip");
        savedTrip.Description.ShouldBe("Updated description");
        savedTrip.StartDate.ShouldBe(newStartDate);
        savedTrip.EndDate.ShouldBe(newEndDate);
    }

    [Fact]
    public async Task UpdateTrip_PartialRequest_PreservesUnspecifiedFields()
    {
        await AddTestUser();

        var createResponse =
            await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);

        var createdTrip =
            await createResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(createdTrip);

        var request = new UpdateTripRequest
        {
            Id = createdTrip.Id,
            Title = "Updated title",
            StartDate = createdTrip.StartDate,
            EndDate = createdTrip.EndDate
        };

        var updateResponse = await HttpClient.PutAsJsonAsync(
            $"/api/trip/{createdTrip.Id}",
            request
        );

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updatedTrip =
            await updateResponse.Content.ReadFromJsonAsync<Trip>();

        Assert.NotNull(updatedTrip);
        updatedTrip.Title.ShouldBe("Updated title");
        updatedTrip.Description.ShouldBe(createdTrip.Description);
        updatedTrip.StartDate.ShouldBe(createdTrip.StartDate);
        updatedTrip.EndDate.ShouldBe(createdTrip.EndDate);
        updatedTrip.UserId.ShouldBe(createdTrip.UserId);
    }

    [Fact]
    public async Task GetTripLocations_MultipleTripsWithPosts_ReturnsOnlyLocationsForGivenTrip()
    {
        await AddTestUser();

        // Create two trips via the API so they're persisted with a valid user
        var trip1Response = await HttpClient.PostAsJsonAsync("/api/trip", tripTestData);
        var trip1 = await trip1Response.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(trip1);

        var trip2Response = await HttpClient.PostAsJsonAsync("/api/trip", new NewTripRequest
        {
            Title = "Second Trip",
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21)
        });
        var trip2 = await trip2Response.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(trip2);

        await using var context = DbContextFactory.CreateDbContext();
        context.Locations.AddRange(
            new Location
            { OsmId = 1, Name = "Location A", DisplayName = "Location A", Lat = "1", Lon = "1", PostId = 1 },
            new Location
            { OsmId = 2, Name = "Location A", DisplayName = "Location A", Lat = "1", Lon = "1", PostId = 2 },
            new Location
            { OsmId = 3, Name = "Location A", DisplayName = "Location A", Lat = "1", Lon = "1", PostId = 3 }
        );

        context.Posts.AddRange(
            new Post { PostId = 1, TripId = trip1.Id, LocationName = "Location A", Title = "Title", OsmId = 1, Content = "Content" },
            new Post { PostId = 2, TripId = trip1.Id, LocationName = "Location B", Title = "Title", OsmId = 2, Content = "Content" },
            new Post { PostId = 3, TripId = trip2.Id, LocationName = "Location C", Title = "Title", OsmId = 3, Content = "Content" }
        );
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _output.WriteLine(ex.InnerException?.Message ?? ex.Message);
            throw;
        }


        var response = await HttpClient.GetAsync($"/api/trip/locations/{trip1.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var locations = await response.Content.ReadFromJsonAsync<List<Location>>();

        // Assert
        Assert.NotNull(locations);
        locations.Count.ShouldBe(2);
        locations.ShouldNotContain(l => l.OsmId == 3);
    }

    [Fact]
    public async Task GetTripCollaborators_NoCollaborators_ReturnsOkResponseWithEmptyList()
    {
        await AddTestUser();

        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21)
        };

        await using var context = DbContextFactory.CreateDbContext();

        context.Trips.Add(trip);

        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/trip/1/collaborators");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var collaborators = await response.Content.ReadFromJsonAsync<List<CollaboratingUser>>();

        collaborators.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTripCollaborators_ValidRequest_ReturnsCorrectly()
    {
        List<CollaboratingUser> mockObject = [
            new() {DisplayName = "Name", UserId = 2},
            new() {DisplayName = "Name 2", UserId = 3}
        ];

        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Returns(true);
        _mockCollaborationService.GetTripCollaborators(1).Returns(mockObject);

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<OkObjectResult>();

        var collaborators = ((OkObjectResult)response.Result).Value;
        collaborators.ShouldNotBeNull();
        collaborators.ShouldBe(mockObject);
    }

    [Fact]
    public async Task GetTripCollaborators_NoClaim_ReturnsUnauthorized()
    {

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_ClaimHasGarbledId_ReturnsUnauthorized()
    {

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "bad-id") };
        var identity = new ClaimsIdentity(claims, "IncorrectAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_TripDoesNotExist_ReturnsNotFound()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Throws<KeyNotFoundException>();

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_UserDoesNotExist_ReturnsNotFound()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Throws<ArgumentException>();

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTripCollaborators_UserHasNoAccess_ReturnsNotFound()
    {
        _mockTripService.UserHasCollaborationAccessToTrip(1, 1).Returns(false);

        var response = await _controller.GetTripCollaborators(1);

        response.Result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

}