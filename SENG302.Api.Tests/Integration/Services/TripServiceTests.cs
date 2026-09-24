using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Integration.Services;

public class TripServiceTests : BaseIntegrationTestFixture
{
    private ITripService ServiceUnderTest => ServiceProvider.GetRequiredService<ITripService>();
    
    private readonly ITripService _tripService;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IUserService _mockUserService;
    public TripServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory)
    {
        _mockUserService = Substitute.For<IUserService>();
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _tripService = new TripService(DbContextFactory, _fakeTimeProvider, _mockUserService);
    }
    
    private async Task<Trip> CreateTestTripWithUserId(int userId)
    {
        _mockUserService.GetUserByIdAsync(userId).Returns(Substitute.For<User>());
        
        NewTripRequest testTripRequest = new NewTripRequest
        {
            Title = "test trip",
            Description = "Description",
            StartDate = DateToDateTimeOffset(2021, 12, 29),
            EndDate = DateToDateTimeOffset(2022, 12, 31),
        };

        var creation = await _tripService.CreateNewTripAsync(testTripRequest, userId);

        Assert.NotNull(creation);

        return creation;
    }
    
    [Fact]
    public async Task CreateNewTrip_TripExists_Success()
    {
        Trip trip = await CreateTestTripWithUserId(1);

        await Should.NotThrowAsync(async () => await _tripService.GetTripByIdAsync(trip.Id));
    }

    [Theory]
    [InlineData("test", 1, "2026-07-04", "2026-07-12")] // Basic Test
    [InlineData("abc", 2, "2026-07-19", "2026-07-29")] // Title with exactly 3 characters
    [InlineData("  xyz  ", 3, "2027-07-19", "2027-07-29")] // Title with 3 characters but whitespace that will be trimmed
    [InlineData("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh", 4, "2000-01-01", "2000-07-29")] // Title with 128 characters
    [InlineData(" 123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh ", 5, "1999-01-01", "1999-03-20")] // Title with 128 characters but whitespace that should be trimmed
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

        DateTimeOffset startDate = DateTimeOffset.Parse(stringStartDate, CultureInfo.CurrentCulture.DateTimeFormat);
        DateTimeOffset endDate = DateTimeOffset.Parse(stringEndDate, CultureInfo.CurrentCulture.DateTimeFormat);

        var tripRequest = new NewTripRequest
        {
            Title = title,
            Description = "Description",
            StartDate = startDate,
            EndDate = endDate,
        };

        // Use the TripService function to create a new trip with the name and user email
        
        var trip = await ServiceUnderTest.CreateNewTripAsync(tripRequest, userId);

        trip.ShouldNotBeNull();
        trip.Title.ShouldBe(title.Trim());
        trip.UserId.ShouldBe(userId);
        trip.Description.ShouldBe("Description");
        trip.StartDate.ShouldBe(startDate);
        trip.EndDate.ShouldBe(endDate);
    }

    [Theory]
    [InlineData("Hi", 1)] // Short title that is less than 3 characters
    [InlineData("Hi!", 2)] // Title with special character
    [InlineData("                    Hi  ", 3)] // long enough but whitespace should be trimmed, meaning not long enough
    [InlineData("          ", 4)] // long enough but only whitespace
    [InlineData("x123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh ", 5)] // Title with 129 characters so invalid
    public async Task CreateNewTrip_InvalidTitle_ThrowValidationException(string title, int userId)
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
            StartDate = DateToDateTimeOffset(2022, 1, 1),
            EndDate = DateToDateTimeOffset(2022, 1, 7),
        };

        // Use the TripService function to create a new trip with the name and user email and check that it throws an ArgumentException
        await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.CreateNewTripAsync(tripRequest, userId));
    }


    [Theory]
    [InlineData("Hi", false)] // Short title that is less than 3 characters
    [InlineData("Hi!", false)] // Title with special character
    [InlineData("                    Hi  ", false)] // long enough but whitespace should be trimmed, meaning not long enough
    [InlineData("          ", false)] // long enough but only whitespace
    [InlineData("x123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890abcdefgh ", false)] // Title with 129 characters so invalid
    [InlineData("Hai", true)] // lower edge
    [InlineData("Hello there this is my trip", true)] // generic case
    [InlineData("00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", true)] // 128 so valid
    public async Task UpdateTrip_VariedTitle_GetExpectedResult(string title, bool expectedResult)
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

        NewTripRequest tripRequest = new()
        {
            Title = "Trip Title",
            Description = "Test",
            StartDate = DateToDateTimeOffset(2022, 1, 1),
            EndDate = DateToDateTimeOffset(2022, 1, 7),
        };

        var trip = await ServiceUnderTest.CreateNewTripAsync(tripRequest, 1);

        UpdateTripRequest updateTripRequest = new UpdateTripRequest
        {
            Id = trip.Id,
            Title = title
        };

        if (expectedResult)
        {
            await Should.NotThrowAsync(async () => await ServiceUnderTest.UpdateTripAsync(updateTripRequest, 1));
        }
        else
        {
            await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.UpdateTripAsync(updateTripRequest, 1));
        }
    }

    [Theory]
    [InlineData(2049)] // just over
    [InlineData(3000)] // way over
    public async Task CreateNewTrip_LongDescription_ThrowValidationException(int descriptionLength)
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
            Description = new string('a', descriptionLength),
            StartDate = DateToDateTimeOffset(2022, 1, 1),
            EndDate = DateToDateTimeOffset(2022, 1, 7),
        };

        // Use the TripService function to create a new trip with the name and user email and check that it throws an ArgumentException
        await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.CreateNewTripAsync(tripRequest, 0));
    }

    [Theory]
    [InlineData(0, true)] // empty
    [InlineData(3, true)] // some
    [InlineData(2048, true)] // edge
    [InlineData(2049, false)] // just over
    [InlineData(3000, false)] // way over
    public async Task UpdateTrip_DescriptionLengthVaries_GetExpectedResult(int descriptionLength, bool expectedResult)
    {
        // Add a user to the database with the email that we are testing with
        await using var context = DbContextFactory.CreateDbContext();
        context.Users.Add(new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordKey = "password",
            Country = "Test Country"
        });
        await context.SaveChangesAsync();

        NewTripRequest tripRequest = new NewTripRequest
        {
            Title = "test",
            Description = "Test",
            StartDate = DateToDateTimeOffset(2022, 1, 1),
            EndDate = DateToDateTimeOffset(2022, 1, 7),
        };

        Trip trip = await ServiceUnderTest.CreateNewTripAsync(tripRequest, 1);

        UpdateTripRequest updateTripRequest = new()
        {
            Id = trip.Id,
            Description = new string('a', descriptionLength)
        };

        if (expectedResult)
        {
            await Should.NotThrowAsync(async () => await ServiceUnderTest.UpdateTripAsync(updateTripRequest, 1));
        }
        else
        {
            await Should.ThrowAsync<MultipleValidationListException>(async () => await ServiceUnderTest.UpdateTripAsync(updateTripRequest, 1));
        }
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
            StartDate = DateToDateTimeOffset(2022, 1, 1),
            EndDate = DateToDateTimeOffset(2021, 12, 29),
        };

        // Use the TripService function to create a new trip with the name and user email and check that it throws an ArgumentException
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

        // Use the TripService function to create a new trip with the name and user email and check that it throws an ArgumentException
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

        // Use the TripService function to retrieve trips for the user email
        var initialTrips = await ServiceUnderTest.GetTripsByUserIdAsync(1);
        initialTrips.Count().ShouldBe(0);

        context.Trips.Add(new Trip
        {
            Title = "Test trip title",
            UserId = 1,
            Description = "Test Description",
            StartDate = DateToDateTimeOffset(2020, 1, 30),
            EndDate = DateToDateTimeOffset(2020, 2, 5)
        });
        await context.SaveChangesAsync();
        var trips = await ServiceUnderTest.GetTripsByUserIdAsync(1);
        trips.Count().ShouldBe(1);
        trips.First().Title.ShouldBe("Test trip title");
        trips.First().Description.ShouldBe("Test Description");
        trips.First().StartDate.ShouldBe(DateToDateTimeOffset(2020, 1, 30));
        trips.First().EndDate.ShouldBe(DateToDateTimeOffset(2020, 2, 5));
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
                StartDate = DateToDateTimeOffset(2026, 7, 1),
                EndDate = DateToDateTimeOffset(2026, 7, 10)
            },
            userId
        );
    }

    [Fact]
    public async Task UpdateTrip_ExistingTrip_PersistsAllChanges()
    {
        var trip = await CreateTripForUpdateTests();

        var newStartDate = DateToDateTimeOffset(2026, 8, 1);
        var newEndDate = DateToDateTimeOffset(2026, 8, 10);

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
    
    [Fact]
    public async Task UserModifyAccessCheck_Valid_Success()
    {
        int userId = 100;
        Trip trip = await CreateTestTripWithUserId(userId);

        Assert.True(await _tripService.UserHasCollaborationAccessToTrip(userId, trip.Id));
    }

    [Fact]
    public async Task UserModifyAccessCheck_NotValid_Fail()
    {
        int userId = 100;
        Trip trip = await CreateTestTripWithUserId(userId);

        Assert.False(await _tripService.UserHasCollaborationAccessToTrip(userId + 1, trip.Id));
    }

    [Fact]
    public async Task UpdateTrip_ValidRequest_UpdatesTrip()
    {
        var trip = await CreateTestTripWithUserId(1);
        var newStartDate = DateToDateTimeOffset(2026, 8, 1);
        var newEndDate = DateToDateTimeOffset(2026, 8, 10);

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            Title = "Updated trip",
            Description = "Updated description",
            StartDate = newStartDate,
            EndDate = newEndDate
        };

        var updatedTrip = await _tripService.UpdateTripAsync(request, 1);

        updatedTrip.Title.ShouldBe("Updated trip");
        updatedTrip.Description.ShouldBe("Updated description");
        updatedTrip.StartDate.ShouldBe(newStartDate);
        updatedTrip.EndDate.ShouldBe(newEndDate);
    }

    [Fact]
    public async Task UpdateTrip_OnlyTitleProvided_UpdatesTitleOnly()
    {
        var trip = await CreateTestTripWithUserId(1);
        var originalDescription = trip.Description;
        var originalStartDate = trip.StartDate;
        var originalEndDate = trip.EndDate;

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            Title = "Updated trip",
            StartDate = originalStartDate,
            EndDate = originalEndDate
        };

        await _tripService.UpdateTripAsync(request, 1);

        var savedTrip = await _tripService.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe("Updated trip");
        savedTrip.Description.ShouldBe(originalDescription);
        savedTrip.StartDate.ShouldBe(originalStartDate);
        savedTrip.EndDate.ShouldBe(originalEndDate);
    }

    [Fact]
    public async Task UpdateTrip_OnlyDescriptionProvided_UpdatesDescriptionOnly()
    {
        var trip = await CreateTestTripWithUserId(1);
        var originalTitle = trip.Title;
        var originalStartDate = trip.StartDate;
        var originalEndDate = trip.EndDate;

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            Description = "Updated description",
            StartDate = originalStartDate,
            EndDate = originalEndDate
        };

        await _tripService.UpdateTripAsync(request, 1);

        var savedTrip = await _tripService.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe(originalTitle);
        savedTrip.Description.ShouldBe("Updated description");
        savedTrip.StartDate.ShouldBe(originalStartDate);
        savedTrip.EndDate.ShouldBe(originalEndDate);
    }

    [Fact]
    public async Task UpdateTrip_OnlyStartDateProvided_UpdatesStartDateOnly()
    {
        var trip = await CreateTestTripWithUserId(1);
        var originalTitle = trip.Title;
        var originalDescription = trip.Description;
        var originalEndDate = trip.EndDate;
        var newStartDate = DateToDateTimeOffset(2022, 10, 10);

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            StartDate = newStartDate,
            EndDate = originalEndDate
        };

        await _tripService.UpdateTripAsync(request, 1);

        var savedTrip = await _tripService.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe(originalTitle);
        savedTrip.Description.ShouldBe(originalDescription);
        savedTrip.StartDate.ShouldBe(newStartDate);
        savedTrip.EndDate.ShouldBe(originalEndDate);
    }

    [Fact]
    public async Task UpdateTrip_OnlyEndDateProvided_UpdatesEndDateOnly()
    {
        var trip = await CreateTestTripWithUserId(1);
        var originalTitle = trip.Title;
        var originalDescription = trip.Description;
        var originalStartDate = trip.StartDate;

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            StartDate = originalStartDate,
            EndDate = DateToDateTimeOffset(2023, 10, 10)
        };

        await _tripService.UpdateTripAsync(request, 1);

        var savedTrip = await _tripService.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe(originalTitle);
        savedTrip.Description.ShouldBe(originalDescription);
        savedTrip.StartDate.ShouldBe(originalStartDate);
        savedTrip.EndDate.ShouldBe(DateToDateTimeOffset(2023, 10, 10));
    }
    
    [Fact]
    public async Task UpdateTrip_EndDateBeforeStartDate_ThrowsValidationException()
    {
        var trip = await CreateTestTripWithUserId(1);

        var originalTitle = trip.Title;
        var originalDescription = trip.Description;
        var originalStartDate = trip.StartDate;
        var originalEndDate = trip.EndDate;

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            Title = "Updated trip",
            Description = "Updated description",
            StartDate = DateToDateTimeOffset(2026, 8, 10),
            EndDate = DateToDateTimeOffset(2026, 8, 1)
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => _tripService.UpdateTripAsync(request, 1)
            );

        exception.Errors.ShouldContainKey("dateTwo");
        exception.Errors["dateTwo"].ShouldContain(
            "End date must not be before the start date"
        );

        var savedTrip = await _tripService.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe(originalTitle);
        savedTrip.Description.ShouldBe(originalDescription);
        savedTrip.StartDate.ShouldBe(originalStartDate);
        savedTrip.EndDate.ShouldBe(originalEndDate);
    }

    [Fact]
    public async Task UpdateTrip_NullStartDate_ThrowsValidationException()
    {
        var trip = await CreateTestTripWithUserId(1);

        var originalTitle = trip.Title;
        var originalDescription = trip.Description;
        var originalStartDate = trip.StartDate;
        var originalEndDate = trip.EndDate;

        var request = new UpdateTripRequest
        {
            Id = trip.Id,
            Title = "Updated trip",
            Description = "Updated description",
            StartDate = null,
            EndDate = DateToDateTimeOffset(2026, 8, 10)
        };

        var exception =
            await Should.ThrowAsync<MultipleValidationListException>(
                () => _tripService.UpdateTripAsync(request, 1)
            );

        exception.Errors.ShouldContainKey("dateOne");
        exception.Errors["dateOne"][0].ShouldContain(
            "Date is invalid"
        );

        var savedTrip = await _tripService.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe(originalTitle);
        savedTrip.Description.ShouldBe(originalDescription);
        savedTrip.StartDate.ShouldBe(originalStartDate);
        savedTrip.EndDate.ShouldBe(originalEndDate);
    }


    [Fact]
    public async Task DeleteTrip_TripDeleted_Success()
    {
        Trip trip = await CreateTestTripWithUserId(1);

        await Should.NotThrowAsync(async () => await _tripService.DeleteTripByIdAsync(trip.Id));

        await Should.ThrowAsync<ArgumentException>(async () => await _tripService.GetTripByIdAsync(trip.Id));
    }

        private readonly NewTripRequest testTripRequest = new NewTripRequest
    {
        Title = "test trip",
        Description = "Description",
        StartDate = DateToDateTimeOffset(2021, 12, 29),
        EndDate = DateToDateTimeOffset(2022, 12, 31),
    };

    [Fact]
    public async Task CreateNewTrip_UserDoesNotExist_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await _tripService.CreateNewTripAsync(testTripRequest, -1));
    }

    [Fact]
    public async Task DeleteTrip_NoTripExists_Fails()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await _tripService.DeleteTripByIdAsync(45));
    }

    [Fact]
    public async Task UpdateTrip_TripDoesNotExist_DoesNotCreateTrip()
    {
        await using var context = DbContextFactory.CreateDbContext();

        var numberOfTripsBefore = await context.Trips.CountAsync();

        var request = new UpdateTripRequest
        {
            Id = 999,
            Title = "Updated trip"
        };

        await Should.ThrowAsync<ArgumentException>(
            () => _tripService.UpdateTripAsync(request, 1)
        );
        await context.SaveChangesAsync();
        await using var context2 = DbContextFactory.CreateDbContext();

        var numberOfTripsAfter = await context2.Set<Trip>().CountAsync();

        numberOfTripsAfter.ShouldBe(numberOfTripsBefore);
    }

    [Fact] 
    public async Task UpdateScrapboardReference_TripHasNoScrapboard_BooleanUpdated()
    {
        var trip = await CreateTestTripWithUserId(1);
        trip.HasScrapboard.ShouldBeFalse();
        await ServiceUnderTest.UpdateScrapboardReferenceAsync(trip.Id);
        var newTrip = await ServiceUnderTest.GetTripByIdAsync(trip.Id);
        newTrip.HasScrapboard.ShouldBeTrue();
    }

    [Fact] 
    public async Task UpdateScrapboardReference_TripHasScrapboard_ThrowArgumentException()
    {
        var trip = await CreateTestTripWithUserId(1);
        trip.HasScrapboard.ShouldBeFalse();
        await ServiceUnderTest.UpdateScrapboardReferenceAsync(trip.Id);
        await Should.ThrowAsync<ArgumentException>(
            () => _tripService.UpdateScrapboardReferenceAsync(trip.Id)
        );

    }

}