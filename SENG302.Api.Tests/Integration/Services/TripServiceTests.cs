using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Services;

public class TripServiceTests : BaseIntegrationTestFixture
{
    private ITripService ServiceUnderTest => ServiceProvider.GetRequiredService<ITripService>();

    public TripServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory) { }

    [Theory]
    [InlineData("test", 1, "2026-07-04", "2026-07-12")] // Basic Test
    [InlineData("abc", 2, "2026-07-19", "2026-07-29")] // Name with exactly 3 characters
    [InlineData("  xyz  ", 3, "2027-07-19", "2027-07-29")] // Name with 3 characters but whitespace that will be trimmed
    [InlineData("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh", 4, "2000-01-01", "2000-07-29")] // Name with 128 characters
    [InlineData(" 123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh ", 5, "1999-01-01", "1999-03-20")] // Name with 128 characters but whitespace that should be trimmed
    [InlineData("f                  f", 6, "1970-07-19", "1970-07-29")] // Lots of spaces but only a few characters at either side
    public async Task CreateNewTrip_Success_ReturnTrip(string title, int userId, string stringStartDate, string stringEndDate)
    {
        // Add a user to the database with the email that we are testing with
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        DateTime startDate = DateTime.Parse(stringStartDate);
        DateTime endDate = DateTime.Parse(stringEndDate);

        var tripRequest = new NewTripRequest
        {
            Title = title,
            Description = "Description",
            StartDate = startDate,
            EndDate = endDate,
        };

        // Use the TaskService function to create a new task list with the name and user email
        var trip = await ServiceUnderTest.CreateNewTripAsync(tripRequest, userId);

        trip.Title.ShouldBe(title.Trim());
        trip.UserId.ShouldBe(userId);
        trip.Description.ShouldBe("Description");
        trip.StartDate.ShouldBe(startDate);
        trip.EndDate.ShouldBe(endDate);
    }

    [Theory]
    [InlineData("Hi", 1)] // Short name that is less than 3 characters
    [InlineData("Hi!", 2)] // Name with special character
    [InlineData("                    Hi  ", 3)] // long enough but whitespace should be trimmed, meaning not long enough
    [InlineData("          ", 4)] // long enough but only whitespace
    [InlineData("x123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh ", 5)] // Name with 129 characters so invalid
    public async Task CreateNewTrip_InvalidName_ThrowValidationException(string title, int userId)
    {
        // Add a user to the database with the email that we are testing with
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        NewTripRequest tripRequest = new NewTripRequest
        {
            Title = title,
            Description = "Test",
            StartDate = DateTime.Parse("2022-01-01"),
            EndDate = DateTime.Parse("2022-01-07"),
        };

        // Use the TaskService function to create a new task list with the name and user email and check that it throws an ArgumentException
        await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.CreateNewTripAsync(tripRequest, userId));
    }

    [Fact]
    public async Task CreateNewTrip_LongDescription_ThrowValidationException()
    {
        // Add a user to the database with the email that we are testing with
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 0,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        NewTripRequest tripRequest = new NewTripRequest
        {
            Title = "test",
            Description = new string('a', 2049),
            StartDate = DateTime.Parse("2022-01-01"),
            EndDate = DateTime.Parse("2022-01-07"),
        };

        // Use the TaskService function to create a new task list with the name and user email and check that it throws an ArgumentException
        await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.CreateNewTripAsync(tripRequest, 0));
    }

    [Fact]
    public async Task CreateNewTrip_EndDateBeforeStartDate_ThrowValidationException()
    {
        // Add a user to the database with the email that we are testing with
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        NewTripRequest tripRequest = new NewTripRequest
        {
            Title = "title",
            Description = "Test",
            StartDate = DateTime.Parse("2022-01-01"),
            EndDate = DateTime.Parse("2021-12-29"),
        };

        // Use the TaskService function to create a new task list with the name and user email and check that it throws an ArgumentException
        await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.CreateNewTripAsync(tripRequest, 1));
    }

    [Fact]
    public async Task CreateNewTrip_NullDates_ThrowValidationException()
    {
        // Add a user to the database with the email that we are testing with
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        NewTripRequest tripRequest = new NewTripRequest
        {
            Title = "title",
            Description = "Test",
            StartDate = null,
            EndDate = null,
        };

        // Use the TaskService function to create a new task list with the name and user email and check that it throws an ArgumentException
        await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.CreateNewTripAsync(tripRequest, 1));
    }


    [Fact]
    public async Task GetTripByUserEmailAsync_RetreiveUsersTrips_Success()
    {
        await using var context = DbContextFactory.CreateDbContext();
        // Add a user to the database with the email that we are testing with
        context.Users.Add(new User
        {
            Id = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        // Use the TaskService function to retrieve task lists for the user email
        var initialTrips = await ServiceUnderTest.GetTripsByUserIdAsync(1);
        initialTrips.Count().ShouldBe(0);

        context.Trips.Add(new Trip
        {
            Title = "Test trip title",
            UserId = 1,
            Description = "Test Description",
            StartDate = new DateTime(2020, 1, 30),
            EndDate = new DateTime(2020, 2, 5)
        });
        await context.SaveChangesAsync();
        var trips = await ServiceUnderTest.GetTripsByUserIdAsync(1);
        trips.Count().ShouldBe(1);
        trips.First().Title.ShouldBe("Test trip title");
        trips.First().Description.ShouldBe("Test Description");
        trips.First().StartDate.ShouldBe(new DateTime(2020, 1, 30));
        trips.First().EndDate.ShouldBe(new DateTime(2020, 2, 5));
    }

    /// <summary>
    /// Creates a trip for the user with the given id and returns it.
    /// This is used for testing purposes to create a trip for update tests
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private async Task<Trip> CreateTripForUpdateTests(int userId = 1)
    {
        await using var context = DbContextFactory.CreateDbContext();

        context.Users.Add(new User
        {
            Id = userId,
            Email = $"test{userId}@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });

        await context.SaveChangesAsync();

        return await ServiceUnderTest.CreateNewTripAsync(
            new NewTripRequest
            {
                Title = "Original trip",
                Description = "Original description",
                StartDate = new DateTime(2026, 7, 1),
                EndDate = new DateTime(2026, 7, 10)
            },
            userId
        );
    }

    [Fact]
    public async Task UpdateTrip_ExistingTrip_PersistsAllChanges()
    {
        var trip = await CreateTripForUpdateTests();

        var newStartDate = new DateTime(2026, 8, 1);
        var newEndDate = new DateTime(2026, 8, 10);

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            Title = "Updated trip",
            Description = "Updated description",
            StartDate = newStartDate,
            EndDate = newEndDate
        };

        await ServiceUnderTest.UpdateTripAsync(request, 1);

        var savedTrip = await ServiceUnderTest.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe("Updated trip");
        savedTrip.Description.ShouldBe("Updated description");
        savedTrip.StartDate.ShouldBe(newStartDate);
        savedTrip.EndDate.ShouldBe(newEndDate);
    }

    [Fact]
    public async Task UpdateTrip_PartialUpdate_PreservesUnspecifiedFields()
    {
        var trip = await CreateTripForUpdateTests();

        var originalDescription = trip.Description;
        var originalStartDate = trip.StartDate;
        var originalEndDate = trip.EndDate;

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            Title = "Updated title",
            StartDate = originalStartDate,
            EndDate = originalEndDate
        };

        await ServiceUnderTest.UpdateTripAsync(request, 1);

        var savedTrip = await ServiceUnderTest.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe("Updated title");
        savedTrip.Description.ShouldBe(originalDescription);
        savedTrip.StartDate.ShouldBe(originalStartDate);
        savedTrip.EndDate.ShouldBe(originalEndDate);
    }

    [Fact]
    public async Task UpdateTrip_NonExistentTrip_ThrowsArgumentException()
    {
        var request = new UpdateTripRequest
        {
            Id = 999,
            Title = "Updated trip"
        };

        await Should.ThrowAsync<ArgumentException>(
            () => ServiceUnderTest.UpdateTripAsync(request, 1)
        );
    }

}