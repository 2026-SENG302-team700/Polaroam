using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using NSubstitute;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Responses;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;
namespace SENG302.Api.Tests.E2E;

public class DeletePostE2ETest : E2ETestFixture
{
    private const string PostTitle = "E2E post deletion test";
    private const long OsmId = 123;

    public DeletePostE2ETest(
        WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Creates the user, trip and post required by each test.
    /// The test authentication handler uses user ID 1.
    /// </summary>
    private async Task<(int TripId, int PostId)> CreatePostAsync()
    {
        GeoServiceMock.GetLocationAsync(OsmId).Returns(
              new GeoLocationSearchResult
              {
                  OsmId = OsmId,
                  Latitude = "-43.532",
                  Longitude = "172.636",
                  Name = "Christchurch",
                  DisplayName = "Christchurch, New Zealand"
              }
          );

        await using var context =
            await DbContextFactory.CreateDbContextAsync();

        var user = new User
        {
            Id = 1,
            Email = "tester@example.com",
            DisplayName = "Test user",
            PasswordKey = "pass",
            Country = "NZ"
        };

        var trip = new Trip
        {
            UserId = user.Id,
            Title = "E2E delete trip",
            Description = "Trip used for deletion tests",
            StartDate = DateToDateTimeOffset(2026, 1, 1),
            EndDate = DateToDateTimeOffset(2026, 1, 10),
        };

        context.Users.Add(user);
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var post = new Post
        {
            TripId = trip.Id,
            Title = PostTitle,
            Content = "Content that should be gone",
            Date = DateTimeOffset.UtcNow.AddDays(-1),
            OsmId = OsmId,
            LocationName = "Christchurch",
            PhotoIds = [],
            CreationTime = DateTimeOffset.Now
        };

        context.Posts.Add(post);
        await context.SaveChangesAsync();

        context.Locations.Add(new SENG302.Api.Models.Responses.Location
        {
            PostId = post.PostId,
            OsmId = OsmId,
            Lat = "-43.532",
            Lon = "172.636",
            Name = "Christchurch",
            DisplayName = "Christchurch, New Zealand"
        });

        await context.SaveChangesAsync();

        return (trip.Id, post.PostId);
    }

    [Fact]
    public async Task ClickingDeleteButton_ShowsConfirmationModal()
    {
        var (tripId, postId) = await CreatePostAsync();

        await Page.GotoAsync(
            $"{AppBaseUrl}/home/trips/{tripId}/posts/{postId}",
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 10000
            }
        );

        await Page.GetByTitle("Delete").ClickAsync();

        var modal = Page.GetByRole(AriaRole.Dialog);

        (await modal.IsVisibleAsync()).ShouldBeTrue();

        var message = modal.GetByText(
            "Are you sure you want to delete this post? " +
            "This action cannot be undone.",
            new() { Exact = true }
        );

        (await message.IsVisibleAsync()).ShouldBeTrue();

        var deleteButton = modal.GetByRole(
            AriaRole.Button,
            new() { Name = "Delete", Exact = true }
        );

        var cancelButton = modal.GetByRole(
            AriaRole.Button,
            new() { Name = "Do not delete", Exact = true }
        );

        (await deleteButton.IsVisibleAsync()).ShouldBeTrue();
        (await cancelButton.IsVisibleAsync()).ShouldBeTrue();
    }
}
