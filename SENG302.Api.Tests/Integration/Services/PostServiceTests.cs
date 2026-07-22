using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Services;

public class PostServiceTests : BaseIntegrationTestFixture
{
    private IPostService ServiceUnderTest => ServiceProvider.GetRequiredService<IPostService>();
    public PostServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory) { }

    private async Task SetupUserAndList(int userId, int listId)
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = userId,
            Email = $"test{userId}@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        context.Trips.Add(new Trip
        {
            Id = listId,
            Title = "Test List",
            UserId = userId,
            Description = "Test",
            StartDate = new DateTime(2017, 10, 08), 
            EndDate = new DateTime(2017, 10, 21)
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllTaskItemsAsync_NoTasks_ReturnsEmpty()
    {
        await SetupUserAndList(1, 1);
        var tasks = await ServiceUnderTest.GetAllPostsAsync(1);
        tasks.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllTaskItemsAsync_WithTasks_ReturnsOnlyUsersTask()
    {
        // setup two users and a task
        await SetupUserAndList(1, 1);
        await SetupUserAndList(2, 2);

        await using var context = DbContextFactory.CreateDbContext();
        context.Posts.Add(new Post
        {
            TripId = 1,
            Title = "User 1 task",
            Content = "desc",
            CurrentStatus = CurrentTaskStatus.Todo,
            creationTime = DateTimeOffset.UtcNow
        });
        context.Posts.Add(new Post
        {
            TripId = 2,
            Title = "User 2 task",
            Content = "desc",
            CurrentStatus = CurrentTaskStatus.Todo,
            creationTime = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var tasks = await ServiceUnderTest.GetAllPostsAsync(1);

        tasks.Count().ShouldBe(1);
        tasks.First().Title.ShouldBe("User 1 task");
    }

    [Fact]
    public async Task CreateNewTaskItemAsync_ValidRequest_ReturnsCreatedTask()
    {
        await SetupUserAndList(1, 1);

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "New task",
            Content = "Some description",
            CurrentStatus = CurrentTaskStatus.Todo,
            Date = null
        };

        var task = await ServiceUnderTest.CreateNewPostAsync(request, 1);

        task.Title.ShouldBe("New task");
        task.TripId.ShouldBe(1);
        task.CurrentStatus.ShouldBe(CurrentTaskStatus.Todo);
    }

    [Theory]
    [InlineData("hi")] // too short
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // too long
    public async Task CreateNewTaskItemAsync_InvalidName_ThrowsValidationException(string name)
    {
        await SetupUserAndList(1, 1);

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = name,
            Content = "desc",
            CurrentStatus = CurrentTaskStatus.Todo,
            Date = null
        };

        await Should.ThrowAsync<MultipleValidationException>(
            async () => await ServiceUnderTest.CreateNewPostAsync(request, 1));
    }

    [Fact]
    public async Task CreateNewTaskItemAsync_PastDueDate_ThrowsValidationException()
    {
        await SetupUserAndList(1, 1);

        var request = new NewPostRequest
        {
            TripId = 1,
            Title = "Valid",
            Content = "desc",
            CurrentStatus = CurrentTaskStatus.Todo,
            Date = DateTime.UtcNow.AddDays(-1)
        };

        await Should.ThrowAsync<MultipleValidationException>(
            async () => await ServiceUnderTest.CreateNewPostAsync(request, 1));
    }

    [Fact]
    public async Task GetTaskItemAsync_ValidId_ReturnsTask()
    {
        await SetupUserAndList(1, 1);

        await using var context = DbContextFactory.CreateDbContext();
        var task = new Post
        {
            TripId = 1,
            Title = "Fetch me",
            Content = "desc",
            CurrentStatus = CurrentTaskStatus.Todo,
            creationTime = DateTimeOffset.UtcNow
        };
        context.Posts.Add(task);
        await context.SaveChangesAsync();

        var result = await ServiceUnderTest.GetPostAsync(task.PostId, 1);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Fetch me");
    }

    [Fact]
    public async Task GetTaskItemAsync_InvalidId_ReturnsNull()
    {
        var result = await ServiceUnderTest.GetPostAsync(-1, 1);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetTaskItemAsync_ProfanityFilterEnabled_CensorsNameAndDescription()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "NZ",
            ProfanityFiltering = true
        });
        context.Trips.Add(new Trip { Id = 1, Title = "Test List", UserId = 1, StartDate = new DateTime(2017, 10, 08), EndDate = new DateTime(2017, 10, 21) });
        var task = new Post
        {
            TripId = 1,
            Title = "shit task",
            Content = "this is shit",
            CurrentStatus = CurrentTaskStatus.Todo,
            creationTime = DateTimeOffset.UtcNow
        };
        context.Posts.Add(task);
        await context.SaveChangesAsync();

        var result = await ServiceUnderTest.GetPostAsync(task.PostId, 1);

        result.ShouldNotBeNull();
        result!.Title.ShouldNotContain("shit");
        result.Content.ShouldNotContain("shit");
    }

    [Fact]
    public async Task GetTaskItemAsync_ProfanityFilterDisabled_DoesntCensorNameAndDescription()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "NZ",
            ProfanityFiltering = false
        });
        context.Trips.Add(new Trip { Id = 1, Title = "Test List", UserId = 1, StartDate = new DateTime(2017, 10, 08), EndDate = new DateTime(2017, 10, 21) });
        var task = new Post
        {
            TripId = 1,
            Title = "shit task",
            Content = "this is shit",
            CurrentStatus = CurrentTaskStatus.Todo,
            creationTime = DateTimeOffset.UtcNow
        };
        context.Posts.Add(task);
        await context.SaveChangesAsync();

        var result = await ServiceUnderTest.GetPostAsync(task.PostId, 1);

        result.ShouldNotBeNull();
        result!.Title.ShouldContain("shit");
        result.Content.ShouldContain("shit");
    }

    [Fact]
    public async Task GetTaskItemsByListAsync_ProfanityFilterEnabled_CensorsTasks()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "NZ",
            ProfanityFiltering = true
        });
        context.Trips.Add(new Trip { Id = 1, Title = "Test List", UserId = 1, StartDate = new DateTime(2017, 10, 08), EndDate = new DateTime(2017, 10, 21) });
        var task = new Post
        {
            TripId = 1,
            Title = "shit task",
            Content = "this is shit",
            CurrentStatus = CurrentTaskStatus.Todo,
            creationTime = DateTimeOffset.UtcNow
        };
        context.Posts.Add(task);
        await context.SaveChangesAsync();

        var results = await ServiceUnderTest.GetPostsByTripAsync(1, 1);

        results.ShouldNotBeEmpty();
        results.First().Title.ShouldNotContain("shit");
        results.First().Content.ShouldNotContain("shit");
    }

    [Fact]
    public async Task GetTaskItemsByListAsync_ProfanityFilterDisabled_DoesntCensorsTasks()
    {
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@test.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "NZ",
            ProfanityFiltering = false
        });
        context.Trips.Add(new Trip { Id = 1, Title = "Test List", UserId = 1, StartDate = new DateTime(2017, 10, 08), EndDate = new DateTime(2017, 10, 21)});
        var task = new Post
        {
            TripId = 1,
            Title = "shit task",
            Content = "this is shit",
            CurrentStatus = CurrentTaskStatus.Todo,
            creationTime = DateTimeOffset.UtcNow
        };
        context.Posts.Add(task);
        await context.SaveChangesAsync();

        var results = await ServiceUnderTest.GetPostsByTripAsync(1, 1);

        results.ShouldNotBeEmpty();
        results.First().Title.ShouldContain("shit");
        results.First().Content.ShouldContain("shit");
    }

    
    [Fact]
    public async Task ReorderTaskItemsAsync_ValidOrdering_SetsOrderingPosition()
    {
        await SetupUserAndList(1, 1);
        
        await using var context = DbContextFactory.CreateDbContext();
        context.Posts.AddRange(
            new Post {
                PostId = 1,
                TripId = 1,
                Title = "task 1",
                Content = ""
            },
            new Post {
                PostId = 2,
                TripId = 1,
                Title = "task 2",
                Content = ""
            },
            new Post {
                PostId = 3,
                TripId = 1,
                Title = "task 3",
                Content = ""
            }
        );
        
        await context.SaveChangesAsync();

        await ServiceUnderTest.ReorderTaskItemsAsync([3, 1, 2]);
        var taskItems = (await ServiceUnderTest.GetPostsByTripAsync(1, 1)).ToList();
        
        taskItems.Count.ShouldBe(3);
        taskItems[0].PostId.ShouldBe(3);
        taskItems[1].PostId.ShouldBe(1);
        taskItems[2].PostId.ShouldBe(2);
    }

    [Fact]
    public async Task ReorderTaskItemsAsync_NoTaskItems_NoChangesInDB()
    {
        await SetupUserAndList(1, 1);
        
        await using var context = DbContextFactory.CreateDbContext();
        context.Posts.Add(
            new Post
            {
                PostId = 1,
                TripId = 1,
                Title = "task 1",
                Content = "",
                OrderPosition = 8
            }
        );
        await context.SaveChangesAsync();
        
        await ServiceUnderTest.ReorderTaskItemsAsync([]);

        var taskItem = await ServiceUnderTest.GetPostAsync(1, 1);
        taskItem.OrderPosition.ShouldBe(8);
    }
}