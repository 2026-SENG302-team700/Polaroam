using System.Net;
using System.Net.Http.Json;
using Reqnroll;
using SENG302.Api.Models.Entities;
using SENG302.Api.Tests.Acceptance.Setup;
using Shouldly;

namespace SENG302.Api.Tests.Acceptance.StepDefinitions;

[Binding]
public class U11PersistDnd
{
    private readonly AcceptanceTestFixture _fixture;
    private HttpResponseMessage? _lastResponse;
    
    public U11PersistDnd(AcceptanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Given(@"I have a task list")]
    public async Task GivenIHaveATaskList()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        context.Trips.Add(new Models.Entities.Trip
        {
            Id = 1,
            Title = "My Task List",
            UserId = _fixture.CurrentUserId!.Value,
            Description = "Test",
            StartDate = new DateTime(2025, 11, 21), 
            EndDate = new DateTime(2025, 12, 21)
        });
        await context.SaveChangesAsync();
    }

    [Given(@"I have 3 tasks \(Task 1, Task 2, Task 3\) in order")]
    public async Task GivenIHave3TasksInOrder()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        
        context.Posts.AddRange(
            new Post
            {
                TripId = 1,
                Title = "Task 1",
                Content = "",
                OrderPosition = 1
            },
            new Post
            {
                TripId = 1,
                Title = "Task 2",
                Content = "",
                OrderPosition = 2
            },
            new Post
            {
                TripId = 1,
                Title = "Task 3",
                Content = "",
                OrderPosition = 3
            }
        );
        
        await context.SaveChangesAsync();
    }

    [When(@"I reorder Task 3 to the top of the list")]
    public async Task WhenIReorderTask3ToTheTopOfTheList()
    {
        await using var context = await _fixture.DbContextFactory.CreateDbContextAsync();
        var task1Id = context.Posts.Single(t => t.Title == "Task 1").PostId;                                                   
        var task2Id = context.Posts.Single(t => t.Title == "Task 2").PostId;                                                   
        var task3Id = context.Posts.Single(t => t.Title == "Task 3").PostId;
        
        _lastResponse = await _fixture.HttpClient.PatchAsJsonAsync("/api/taskItem/order", new List<int> 
        {
            task3Id, task1Id, task2Id
        });
    }
    
    [Then(@"The tasks are returned in the order \(Task 3, Task 1, Task 2\)")]                                                     
    public async Task ThenTasksAreReturnedInOrder()
    {
        _lastResponse!.StatusCode.ShouldBe(HttpStatusCode.OK);
                                                                                                                                
        var response = await _fixture.HttpClient.GetAsync("/api/post");
        var tasks = await response.Content.ReadFromJsonAsync<List<Post>>();                                                   
        tasks![0].Title.ShouldBe("Task 3");                                                                                        
        tasks[1].Title.ShouldBe("Task 1");
        tasks[2].Title.ShouldBe("Task 2");                                                                                         
    } 
}