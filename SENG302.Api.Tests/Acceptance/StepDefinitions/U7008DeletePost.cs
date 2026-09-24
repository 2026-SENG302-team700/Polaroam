using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7005DeletePost
{
    private readonly AcceptanceTestFixture _fixture;

    private int _postId;
    private int _tripId;
    private string _photoId;
    private HttpResponseMessage? _deleteResponse;

    public U7005DeletePost(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }



    [Given("I have an existing post to delete")]
    public async Task GivenIHaveAnExistingPostToDelete()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var user = new User
        {
            Id = 1,
            Email = "delete@example.com",
            DisplayName = "Postowner",
            PasswordKey = "password",
            Country = "NZ"
        };

        var trip = new Trip
        {
            Id = 1,
            UserId = user.Id,
            Title = "Trip with post",
            Description = "Test trip",
            StartDate = DateToDateTimeOffset(2026, 1, 1),
            EndDate = DateToDateTimeOffset(2026, 1, 10),
        };

        var post = new Post
        {
            TripId = trip.Id,
            Title = "Post to delete",
            Content = "Post content",
            OsmId = 1,
            OsmType = "node",
            LocationName = "Christchurch",
            PhotoIds = [],
            Date = DateTimeOffset.UtcNow.AddDays(-1),
            CreationTime = DateTimeOffset.UtcNow
        };

        context.Users.Add(user);
        context.Trips.Add(trip);
        context.Posts.Add(post);

        await context.SaveChangesAsync();

        var location = new Location
        {
            PostId = post.PostId,
            OsmId = post.OsmId,
            OsmType = post.OsmType,
            Lat = "-43.532",
            Lon = "172.636",
            Name = "Christchurch",
            DisplayName = "Christchurch, New Zealand"
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync();

        _postId = post.PostId;
        _tripId = trip.Id;
    }

    [Given("I am viewing a trip and the trip contains posts")]
    public async Task GivenIAmViewingTripWithPosts()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var user = new User
        {
            Id = 1,
            Email = "delete@example.com",
            DisplayName = "Postowner",
            PasswordKey = "password",
            Country = "NZ"
        };

        var trip = new Trip
        {
            Id = 1,
            UserId = user.Id,
            Title = "Trip with post",
            Description = "Test trip",
            StartDate = DateToDateTimeOffset(2026, 1, 1),
            EndDate = DateToDateTimeOffset(2026, 1, 10)
        };

        var post = new Post
        {
            TripId = trip.Id,
            Title = "Post to delete",
            Content = "Post content",
            OsmId = 1,
            OsmType = "node",
            LocationName = "Christchurch",
            PhotoIds = [],
            Date = DateTimeOffset.UtcNow.AddDays(-1),
            CreationTime = DateTimeOffset.UtcNow
        };

        context.Users.Add(user);
        context.Trips.Add(trip);
        context.Posts.Add(post);

        await context.SaveChangesAsync();

        var location = new Location
        {
            PostId = post.PostId,
            OsmId = post.OsmId,
            OsmType = post.OsmType,
            Lat = "-43.532",
            Lon = "172.636",
            Name = "Christchurch",
            DisplayName = "Christchurch, New Zealand"
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync();


        var fileBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
        content.Add(fileContent, "files", "panda.webp"); // field name "files" matches formData.append("files", ...)

        content.Add(new StringContent("1"), "postId");
        content.Add(new StringContent("1"), "tripId");

        var response = await _fixture.HttpClient.PutAsync("/api/post/images", content);
        var photoId = await response.Content.ReadAsStringAsync();

        _photoId = photoId;
        _postId = post.PostId;
        _tripId = trip.Id;
    }

    [When("I delete the tripp")]
    public async Task WhenIDeleteTheTrip()
    {
        await _fixture.HttpClient.DeleteAsync($"api/trip/{_tripId}");
    }

    [When("I confirm deletion of the post")]
    public async Task WhenIConfirmDeletionOfThePost()
    {
        _deleteResponse =
            await _fixture.HttpClient.DeleteAsync(
                $"api/post/{_postId}");
    }

    [Then("the posts are removed")]
    public async Task ThenPostAreRemoved()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var postExists = await context.Posts.AnyAsync(p => p.PostId == _postId);
        postExists.ShouldBe(false);
    }

    [Then("their contents within trip are deleted as well")]
    public async Task ThenItsContentsAreRemoved()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        int numericPhotoId;
        int.TryParse(_photoId, out numericPhotoId);
        var locationExists = await context.Locations.AnyAsync(p => p.PostId == _postId);
        var imageExists = await context.CustomFiles.AnyAsync(p => p.Id == numericPhotoId);

        locationExists.ShouldBe(false);
        imageExists.ShouldBe(false);
    }

    [Then("the post deletion is successful")]
    public void ThenThePostDeletionIsSuccessful()
    {
        _deleteResponse.ShouldNotBeNull();
        _deleteResponse.StatusCode
            .ShouldBe(HttpStatusCode.NoContent);
    }

    [Then("the post is removed from the database")]
    public async Task ThenThePostIsRemovedFromTheDatabase()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var postExists = await context.Posts
            .AnyAsync(post => post.PostId == _postId);

        postExists.ShouldBeFalse();
    }

    [Then("the post location is removed from the database")]
    public async Task ThenThePostLocationIsRemoved()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var locationExists = await context.Locations
            .AnyAsync(location => location.PostId == _postId);

        locationExists.ShouldBeFalse();
    }

    [Then("the trip containing the post remains")]
    public async Task ThenTheTripContainingThePostRemains()
    {
        await using var context =
            await _fixture.DbContextFactory.CreateDbContextAsync();

        var tripExists = await context.Trips
            .AnyAsync(trip => trip.Id == _tripId);

        tripExists.ShouldBeTrue();
    }
}
