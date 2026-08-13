using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class ImageControllerTests : BaseIntegrationTestFixture
{
    public ImageControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory){}


    [Fact]
    public async Task GetPhotoTags_ValidId_ReturnsOk()
    {
        List<string> tags = ["Berlin", "German", "Bratwurst", "Volkswagen", "Lederhosen"];
        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
            Tags = tags
        });
        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newTags = await response.Content.ReadFromJsonAsync<List<string>>();
        newTags.ShouldNotBeNull();
        foreach (var tag in newTags)
        {
            tags.ShouldContain(tag);
        }
    }

    [Fact]
    public async Task GetPhotoTags_ValidIdInQueue_ReturnsOk()
    {

        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
            InTagQueue = true
        });
        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("Tags are being generated.");
    }
    
    [Fact]
    public async Task GetPhotoTags_ValidIdNoTags_ReturnsNoContent()
    {

        await using var context = DbContextFactory.CreateDbContext();

        // Add user to DB
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateOnly(2025, 11, 21), EndDate = new DateOnly(2025, 12, 21) });
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", OsmId = 292806332, LocationName = "Christchurch", });
        context.CustomFiles.Add(new CustomFile
        {
            Id = 1,
            OwnerId = 1,
            FileKey = "abc",
            OriginalFileName = "Du Bist Gut.Genug",
            MimeType = "Genug",
        });

        await context.SaveChangesAsync();
        var response = await HttpClient.GetAsync("/api/image/1/tags");
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

    }


}