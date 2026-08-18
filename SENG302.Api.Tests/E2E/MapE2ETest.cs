using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using SENG302.Api.Models.Entities;
using System.Text.RegularExpressions;
using Shouldly;

namespace SENG302.Api.Tests.E2E;

public class TripMapE2ETest : E2ETestFixture
{
    public TripMapE2ETest(
        WebApplicationFactory<Program> factory
    ) : base(factory)
    {
    }

    /// <summary>
    /// Creates a trip containing one geotagged post for map testing.
    /// </summary>
    private async Task<(int TripId, int PostId)> CreateTripWithPostAsync()
    {
        await using var context =
            await DbContextFactory.CreateDbContextAsync();

        var user = new User
        {
            Id = 1,
            Email = "map-test@example.com",
            DisplayName = "Map tester",
            PasswordKey = "password",
            Country = "NZ"
        };

        var trip = new Trip
        {
            UserId = user.Id,
            Title = "E2E map trip",
            Description = "Trip used for map E2E testing",
            StartDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero)
        };

        context.Users.Add(user);
        context.Trips.Add(trip);

        await context.SaveChangesAsync();

        var post = new Post
        {
            TripId = trip.Id,
            Title = "Christchurch post",
            Content = "A post located in Christchurch.",
            Date = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero),
            OsmId = 123,
            OsmType = "node",
            LocationName = "Christchurch",
            PhotoIds = [],
            CreationTime = DateTimeOffset.UtcNow
        };

        context.Posts.Add(post);
        await context.SaveChangesAsync();

        context.Locations.Add(new SENG302.Api.Models.Responses.Location
        {
            OsmId = post.OsmId,
            OsmType = post.OsmType,
            PostId = post.PostId,
            Lat = "-43.5321",
            Lon = "172.6362",
            Name = "Christchurch",
            DisplayName = "Christchurch, Canterbury, New Zealand"
        });

        await context.SaveChangesAsync();

        return (trip.Id, post.PostId);
    }

    [Fact]
    public async Task TripPage_WithPost_DisplaysMapAndMarker()
    {
        var (tripId, postId) = await CreateTripWithPostAsync();

        await Page.GotoAsync(
            $"{AppBaseUrl}/home/trips/{tripId}",
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 10000
            }
        );

        var map = Page.GetByTestId("trip-map");
        var marker = Page.GetByTestId($"post-marker-{postId}");

        await map.WaitForAsync(
            new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            }
        );

        await marker.WaitForAsync(
            new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            }
        );

        (await map.IsVisibleAsync()).ShouldBeTrue();
        (await marker.IsVisibleAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task HoveringPostCard_ShowsAssociatedMapMarker()
    {
        var (tripId, postId) = await CreateTripWithPostAsync();

        await Page.GotoAsync(
            $"{AppBaseUrl}/home/trips/{tripId}",
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 10000
            }
        );

        var postCard = Page.GetByTestId($"post-card-{postId}");
        var marker = Page.GetByTestId($"post-marker-{postId}");

        await postCard.WaitForAsync(
            new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            }
        );

        await marker.WaitForAsync(
            new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            }
        );

        var initialClasses =
            await marker.GetAttributeAsync("class") ?? "";

        initialClasses.ShouldNotContain("if-highlighted");

        await postCard.HoverAsync();

        await Assertions.Expect(marker).ToHaveClassAsync(
            new Regex("if-highlighted"),
            new LocatorAssertionsToHaveClassOptions
            {
                Timeout = 3000
            }
        );

    }
}
