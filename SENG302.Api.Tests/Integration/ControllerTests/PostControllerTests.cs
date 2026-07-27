using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;




public class PostControllerTests : BaseIntegrationTestFixture
{
    public PostControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory)
    {
    }

    [Fact]
    public async Task CreatePost_ValidData_Ok()
    {
        // Create DB
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

        await context.SaveChangesAsync();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "TripId");
        content.Add(new StringContent("name"), "Title");
        content.Add(new StringContent("test"), "Content");
        content.Add(new StringContent("France"), "Location");
        content.Add(new StringContent(DateTime.Now.ToString("o")), "Date");

        var message = await HttpClient.PostAsync("/api/post", content);
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }


    [Fact]
    public async Task GetAllPosts_AuthenticatedUser_ReturnsOnlyUsersPost()
    {
        await using var context = DbContextFactory.CreateDbContext();
        // Add two users
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Users.Add(new User { Id = 2, Email = "other@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });

        // Add a trip to each user
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });
        context.Trips.Add(new Trip { Id = 2, Title = "Other trip", UserId = 2, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });

        // Add 1 post to each trip
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", Location = "location 1" });
        context.Posts.Add(new Post { TripId = 2, Title = "Ti  tle", Content = "", Location = "location 1" });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var posts = await response.Content.ReadFromJsonAsync<List<Post>>();
        posts!.Count.ShouldBe(1);
        posts.First().Title.ShouldBe("My post");
    }

    [Fact]
    public async Task GetAllPosts_NoPosts_ReturnsEmpty()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test User", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My Trip", UserId = 1, Description = "", StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var posts = await response.Content.ReadFromJsonAsync<List<Post>>();
        posts!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllPostsFromTrip_ValidRequest_ReturnsPostsFromRequestedTripOnly()
    {
        await using var context = DbContextFactory.CreateDbContext();
        // Add two users
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Users.Add(new User { Id = 2, Email = "other@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });

        // Add a trip to each user
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });
        context.Trips.Add(new Trip { Id = 2, Title = "Other trip", UserId = 2, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });

        // Add 1 post to each trip
        context.Posts.Add(new Post { TripId = 1, Title = "My post", Content = "", Location = "location 1" });
        context.Posts.Add(new Post { TripId = 2, Title = "Ti  tle", Content = "", Location = "location 1" });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/trip/post/1");
        var posts = await response.Content.ReadFromJsonAsync<List<Post>>();

        posts.ShouldNotBeNull();
        posts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task UploadPostPhotos_ValidRequest_PostContainsPhotos()
    {
        await using var context = DbContextFactory.CreateDbContext();
        
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 1, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });
        context.Posts.Add(new Post {TripId = 1, Title = "My post", Content = "", PhotoIds = [], Location = "location 1" });
        await context.SaveChangesAsync();
        
        
        var fileBytes = await File.ReadAllBytesAsync("resources/panda.webp");

        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
        content.Add(fileContent, "files", "panda.webp"); // field name "files" matches formData.append("files", ...)

        content.Add(new StringContent("1"), "postId");
        content.Add(new StringContent("1"), "tripId");
        
        var response = await HttpClient.PutAsync("/api/post/images", content);
        var photoIds = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        photoIds.ShouldBe("[1]");
        
    }
    
    [Fact]
    public async Task UploadPostPhotos_InvalidRequest_NotOwnerOfPost()
    {
        await using var context = DbContextFactory.CreateDbContext();
        
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Users.Add(new User { Id = 2, Email = "other@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My trip", UserId = 2, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });
        context.Posts.Add(new Post {TripId = 1, Title = "My post", Content = "", PhotoIds = [], Location = "location 1" });
        await context.SaveChangesAsync();
        
        
        var fileBytes = await File.ReadAllBytesAsync("resources/panda.webp");

        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
        content.Add(fileContent, "files", "panda.webp"); // field name "files" matches formData.append("files", ...)
        
        content.Add(new StringContent("1"), "postId");
        content.Add(new StringContent("1"), "tripId");
        
        
        var response = await HttpClient.PutAsync("/api/post/images", content);
        var photoIds = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        photoIds.ShouldContain("You do not have authorisation to upload photos to this post!");

    }
}