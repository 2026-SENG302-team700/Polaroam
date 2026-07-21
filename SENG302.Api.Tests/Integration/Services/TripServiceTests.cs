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
    [InlineData("test", 1,  "2026-07-04", "2026-07-12")] // Basic Test
    [InlineData("abc", 2,  "2026-07-19", "2026-07-29")] // Name with exactly 3 characters
    [InlineData("  xyz  ", 3,  "2027-07-19", "2027-07-29")] // Name with 3 characters but whitespace that will be trimmed
    [InlineData("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh", 4,  "2000-01-01", "2000-07-29")] // Name with 128 characters
    [InlineData(" 123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh ", 5,  "1999-01-01", "1999-03-20")] // Name with 128 characters but whitespace that should be trimmed
    [InlineData("f                  f", 6,  "1970-07-19", "1970-07-29")] // Lots of spaces but only a few characters at either side
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
            Title = title, Description = "Description", StartDate = startDate, EndDate = endDate,
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
    public async Task CreateNewTrip_InvalidName_ThrowArgumentException(string title, int userId)
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
            Title = title, Description = "Test", StartDate = DateTime.Parse("2022-01-01"), EndDate = DateTime.Parse("2022-01-07"),
        };
        
        // Use the TaskService function to create a new task list with the name and user email and check that it throws an ArgumentException
        await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.CreateNewTripAsync(tripRequest, userId));
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
        trips.First().StartDate.ShouldBe( new DateTime(2020, 1, 30));
        trips.First().EndDate.ShouldBe( new DateTime(2020, 2, 5));
    }

}