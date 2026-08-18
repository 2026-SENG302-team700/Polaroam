using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.E2E;

public class SearchImageNavigationE2ETest : E2ETestFixture
{
    private const string SearchTag = "searchable";
    private const string TripTitle = "Search E2E trip";
    private const string PostTitle = "Search E2E post";

    public SearchImageNavigationE2ETest(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Creates a tagged album image and optionally associates it with a post.
    /// </summary>
    private async Task<(int TripId, int? PostId)> CreateSearchableImageAsync(
        bool belongsToPost)
    {
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            context.Users.Add(new User
            {
                Id = 1,
                Email = "search-e2e@example.com",
                DisplayName = "Search E2E user",
                PasswordKey = "password",
                Country = "NZ"
            });

            context.Trips.Add(new Trip
            {
                Id = 1,
                UserId = 1,
                Title = TripTitle,
                Description = "Trip used for image search E2E tests",
                StartDate = DateToDateTimeOffset(2026, 1, 1),
                EndDate = DateToDateTimeOffset(2026, 1, 10)
            });
            context.Albums.Add(new Album { TripId = 1, PhotoIds = [] });
            await context.SaveChangesAsync();
        }

        var imageBytes = await File.ReadAllBytesAsync("resources/panda.webp");
        await using var stream = new MemoryStream(imageBytes);
        var image = new FormFile(stream, 0, imageBytes.Length, "file", "panda.webp")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/webp"
        };

        var fileService = ServiceProvider.GetRequiredService<IFileService>();
        var savedImage = await fileService.SaveFileAsync(image, ownerId: 1);

        int? postId = null;
        await using (var context = await DbContextFactory.CreateDbContextAsync())
        {
            var storedImage = await context.CustomFiles.FindAsync(savedImage.Id);
            storedImage!.Tags = [SearchTag];

            var album = await context.Albums.SingleAsync(a => a.TripId == 1);
            album.PhotoIds.Add(savedImage.Id);

            if (belongsToPost)
            {
                var post = new Post
                {
                    TripId = 1,
                    Title = PostTitle,
                    Content = "Post containing the searched image",
                    Date = DateToDateTimeOffset(2026, 1, 5),
                    OsmId = 1,
                    LocationName = "Christchurch",
                    PhotoIds = [savedImage.Id],
                    CreationTime = DateTimeOffset.UtcNow
                };
                context.Posts.Add(post);
                await context.SaveChangesAsync();
                postId = post.PostId;
            }
            else
            {
                await context.SaveChangesAsync();
            }
        }

        return (1, postId);
    }

    private async Task OpenSearchResultImageAsync()
    {
        await Page.GotoAsync(
            $"{AppBaseUrl}/home",
            new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 10000
            });

        var searchInput = Page.GetByPlaceholder("search your images");
        try
        {
            await searchInput.FillAsync(SearchTag, new LocatorFillOptions
            {
                Timeout = 10000
            });
        }
        catch
        {
            await CaptureFailureAsync("SearchImageNavigationE2ETest");
            throw;
        }
        var image = Page.Locator(".photo-trip-body .clickable-image").First;
        await image.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10000
        });
        await image.ClickAsync();

        await Page.Locator(".fullscreen-image-wrapper").WaitForAsync(
            new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            });
    }

    [Fact]
    public async Task SearchResultImage_OpensEnlargedViewAndNavigatesToTripAlbum()
    {
        var (tripId, _) = await CreateSearchableImageAsync(belongsToPost: false);

        await OpenSearchResultImageAsync();

        await Assertions.Expect(
            Page.GetByText($"trip: {TripTitle}", new() { Exact = true }))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

        var tripAlbumButton = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Go to Trip Album", Exact = true });
        await Assertions.Expect(tripAlbumButton).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        await tripAlbumButton.ClickAsync();
        await Page.WaitForURLAsync($"**/home/trips/{tripId}/album");

        Page.Url.ShouldEndWith($"/home/trips/{tripId}/album");
    }

    [Fact]
    public async Task PostSearchResultImage_OpensEnlargedViewAndNavigatesToPost()
    {
        var (tripId, postId) = await CreateSearchableImageAsync(
            belongsToPost: true);

        await OpenSearchResultImageAsync();

        await Assertions.Expect(
            Page.GetByText($"post: {PostTitle}", new() { Exact = true }))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 10000
            });

        var postButton = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Go to Post", Exact = true });
        await Assertions.Expect(postButton).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });

        await postButton.ClickAsync();
        await Page.WaitForURLAsync($"**/home/trips/{tripId}/posts/{postId}");

        Page.Url.ShouldEndWith($"/home/trips/{tripId}/posts/{postId}");
    }
}
