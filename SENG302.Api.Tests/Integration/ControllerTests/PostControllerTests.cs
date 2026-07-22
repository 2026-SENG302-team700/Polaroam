using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SENG302.Api.Models.Entities;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class PostControllerTests : BaseIntegrationTestFixture
{
    public PostControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory) { }

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
        context.Trips.Add(new Trip
        {
            Id = 1,
            Title = "test tasklist",
            UserId = 1,
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        });
        
        await context.SaveChangesAsync();

        var message = await HttpClient.PostAsJsonAsync("/api/post", new
        {
            taskListId = 1,
            name = "name",
            description = "test",
            dueDate = DateTime.UtcNow.Add(TimeSpan.FromDays(1)),
            location = "France"
        });
        message.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
    

    [Fact]
    public async Task GetAllTasks_AuthenticatedUser_ReturnsOnlyUsersTask()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test user", PasswordKey = "password", Country = "NZ" });
        context.Users.Add(new User { Id = 2, Email = "other@example.com", DisplayName = "Other user", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My list", UserId = 1, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21)});
        context.Trips.Add(new Trip { Id = 2, Title = "Other list", UserId = 2, StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21) });
        context.Posts.Add(new Post { PostId = 0, TripId = 1, Title = "My task", Content = "", Location = "location 1"});
        context.Posts.Add(new Post { PostId = 1, TripId = 1, Title = "Ti  tle", Content = "", Location = "location 1" });
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<Post>>();
        tasks!.Count.ShouldBe(1);
        tasks.First().Title.ShouldBe("My task");
    }

    [Fact]
    public async Task GetAllTasks_NoTasks_ReturnsEmptyList()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User { Id = 1, Email = "test@example.com", DisplayName = "Test User", PasswordKey = "password", Country = "NZ" });
        context.Trips.Add(new Trip { Id = 1, Title = "My List", UserId = 1, Description = "", StartDate = new DateTime(2025, 11, 21), EndDate = new DateTime(2025, 12, 21)});
        await context.SaveChangesAsync();

        var response = await HttpClient.GetAsync("/api/post");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<Post>>();
        tasks!.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task OrderTaskItems_ValidIds_ReturnsOk()
    {                                                                                                                             
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User 
            { 
                Id = 1, 
                Email = "test@example.com", 
                DisplayName = "test",
                PasswordKey = "password",
                Country = "NZ" 
            }
        );                                                                                                                           
        context.Trips.Add(new Trip
            {
                Id = 1, 
                Title = "test list",
                UserId = 1,
                StartDate = new DateTime(2025, 11, 21), 
                EndDate = new DateTime(2025, 12, 21)
            }
        );
        context.Posts.AddRange(                                                                                               
            new Post 
            { 
                PostId = 1, 
                TripId = 1, 
                Title = "task 1",
                Location = "location 1",
                Content = ""  
            },                                                                                                                            
            new Post
            {
                PostId = 2, 
                TripId = 1, 
                Title = "task 2", 
                Content = "",
                Location = "location 2"
            }
        );                                                                                                                        
        await context.SaveChangesAsync();
                                                                                                                                
        var response = await HttpClient.PatchAsJsonAsync("/api/taskItem/order", new List<int> { 2, 1 });                          
   
        response.StatusCode.ShouldBe(HttpStatusCode.OK);                                                                          
    }    
    
    [Fact]          
    public async Task OrderTaskItems_ReorderTasks_ReturnsNewOrder()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User 
            { 
                Id = 1, 
                Email = "test@example.com", 
                DisplayName = "test", 
                PasswordKey = "password", 
                Country = "NZ" 
            }
        );                                                                                                                           
        context.Trips.Add(new Trip
            {
                Id = 1, 
                Title = "test list", 
                UserId = 1,
                StartDate = new DateTime(2025, 11, 21), 
                EndDate = new DateTime(2025, 12, 21)
            }
        );                                             
        context.Posts.AddRange(                                                                                               
            new Post
            {
                PostId = 1, 
                TripId = 1, 
                Title = "task 1",
                Location = "location 1",
                Content = ""
            },
            new Post
            {
                PostId = 2, 
                TripId = 1, 
                Title = "task 2",
                Location = "location 2",
                Content = ""
            }  
        );                                                                                                                        
        await context.SaveChangesAsync();                                                                                         
                                                                                                                                
        await HttpClient.PatchAsJsonAsync("/api/taskItem/order", new List<int> { 2, 1 });                                         
   
        var getResponse = await HttpClient.GetAsync("/api/post");                                                             
        var tasks = await getResponse.Content.ReadFromJsonAsync<List<Post>>();
        tasks[0].PostId.ShouldBe(2);
        tasks[1].PostId.ShouldBe(1);
    }

}