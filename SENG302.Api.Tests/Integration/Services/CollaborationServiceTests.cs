using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.Services;

public class CollaborationServiceTests : BaseIntegrationTestFixture
{
    private ICollaborationService ServiceUnderTest => ServiceProvider.GetRequiredService<ICollaborationService>();
    private IUserService _mockUserService;

    public CollaborationServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory)
    {
        _mockUserService = Substitute.For<IUserService>();
    }
    
    
    /// <summary>
    /// Creates and adds to the database a test user for these tests
    /// </summary>
    private async Task AddTestUser()
    {

        User testUser = new()
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        };
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(testUser);
        await context.SaveChangesAsync();

        _mockUserService.GetUserByIdAsync(testUser.Id).Returns(Task.FromResult((User?)testUser));
    }

    [Fact]
    public async Task GetTripCollaborators_NoCollaborators_ReturnEmptyList()
    {
        await AddTestUser();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21)
        };

        await using var context = DbContextFactory.CreateDbContext();

        context.Trips.Add(trip);

        await context.SaveChangesAsync();

        var collaborators = await ServiceUnderTest.GetTripCollaborators(1);
        
        collaborators.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task GetTripCollaborators_TwoCollaborators_ReturnListWithTwoCollaboratorIds()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();

        User user2 = new()
        {
            
            Id = 2,
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            PasswordKey = "password",
            Country = "Test Country"
            
        };
        
        User user3 = new()
        {
            Id = 3,
            Email = "test3@example.com",
            DisplayName = "Test User 3",
            PasswordKey = "password",
            Country = "Test Country"
        };
        
        context.Users.Add(user2);
        context.Users.Add(user3);

        await context.SaveChangesAsync();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2, 3]
        };

        context.Trips.Add(trip);
        
        await context.SaveChangesAsync(); 

        var collaborators = await ServiceUnderTest.GetTripCollaborators(1);
        
        collaborators.Count.ShouldBe(2);
    }
    
    [Fact]
    public async Task GetTripCollaborators_TripDoesNotExist_ReturnsKeyNotFoundException()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 1,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2, 3]
        };

        context.Trips.Add(trip);
        
        await context.SaveChangesAsync(); 
        

        await Should.ThrowAsync<KeyNotFoundException>(
            async () => await ServiceUnderTest.GetTripCollaborators(4)
        );
    }
    
    [Fact]
    public async Task GetTripCollaborators_OwnerDoesNotExist_ReturnsKeyNotFoundException()
    {
        await AddTestUser();
        
        await using var context = DbContextFactory.CreateDbContext();
        
        var trip = new Trip
        {
            Title = "Trip!",
            UserId = 3,
            Description = "Test",
            StartDate = DateToDateTimeOffset(2025, 11, 21),
            EndDate = DateToDateTimeOffset(2025, 12, 21),
            Collaborators = [2, 3]
        };

        context.Trips.Add(trip);
        
        await context.SaveChangesAsync(); 
        

        await Should.ThrowAsync<KeyNotFoundException>(
            async () => await ServiceUnderTest.GetTripCollaborators(1)
        );
    }

}