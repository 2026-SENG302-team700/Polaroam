using System.Net;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U7005DeletePost
{
    private readonly AcceptanceTestFixture _fixture;

    private int _postId;
    private int _tripId;
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
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 10)
        };

        var post = new Post
        {
            TripId = trip.Id,
            Title = "Post to delete",
            Content = "Post content",
            OsmId = 123,
            PhotoIds = [],
            Date = DateTimeOffset.Now.AddDays(-1),
            creationTime = DateTimeOffset.Now
        };

        context.Users.Add(user);
        context.Trips.Add(trip);
        context.Posts.Add(post);

        await context.SaveChangesAsync();

        var location = new Location
        {
            PostId = post.PostId,
            OsmId = post.OsmId,
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

    [When("I confirm deletion of the post")]
    public async Task WhenIConfirmDeletionOfThePost()
    {
        _deleteResponse =
            await _fixture.HttpClient.DeleteAsync(
                $"api/post/{_postId}");
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