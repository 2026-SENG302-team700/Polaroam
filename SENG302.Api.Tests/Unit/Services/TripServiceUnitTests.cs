using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.Core.Arguments;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using Shouldly;
using static SENG302.Api.Helpers.DateHelper;

namespace SENG302.Api.Tests.Unit.Services;

public class TripServiceUnitTest : DatabaseUnitTestFixture
{
    private readonly NewTripRequest testTripRequest = new NewTripRequest
    {
        Title = "test trip",
        Description = "Description",
        StartDate = DateToDateTimeOffset(2021, 12, 29),
        EndDate = DateToDateTimeOffset(2022, 12, 31),
    };

    private readonly ITripService _tripService;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IUserService _mockUserService;
    public TripServiceUnitTest()
    {
        _mockUserService = Substitute.For<IUserService>();
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _tripService = new TripService(_mockDbContextFactory, _fakeTimeProvider, _mockUserService);
    }

    private async Task<Trip> CreateTestTripWithUserId(int userId)
    {
        _mockUserService.GetUserByIdAsync(userId).Returns(Substitute.For<User>());

        var creation = await _tripService.CreateNewTripAsync(testTripRequest, userId);

        Assert.NotNull(creation);

        return creation;
    }

    [Fact]
    public async Task CreateNewTrip_UserDoesNotExist_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await _tripService.CreateNewTripAsync(testTripRequest, -1));
    }

    [Fact]
    public async Task CreateNewTrip_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        NewTripRequest newTripRequest = new NewTripRequest
        {
            Title = "test trip",
            Description = "Description",
            StartDate = DateToDateTimeOffset(2022, 12, 29),
            EndDate = DateToDateTimeOffset(2022, 11, 30),
        };

        await Should.ThrowAsync<MultipleValidationListException>(async () => await _tripService.CreateNewTripAsync(newTripRequest, -1));
    }

    [Fact]
    public async Task CreateNewTrip_NullDates_ThrowsArgumentException()
    {
        NewTripRequest newTripRequest = new NewTripRequest
        {
            Title = "test trip",
            Description = "Description",
            StartDate = null,
            EndDate = null,
        };

        await Should.ThrowAsync<MultipleValidationListException>(async () => await _tripService.CreateNewTripAsync(newTripRequest, -1));
    }

    [Fact]
    public async Task CreateNewTrip_TripExists_Success()
    {
        Trip trip = await CreateTestTripWithUserId(1);

        await Should.NotThrowAsync(async () => await _tripService.GetTripByIdAsync(trip.Id));
    }

    [Fact]
    public async Task DeleteTrip_TripDeleted_Success()
    {
        Trip trip = await CreateTestTripWithUserId(1);

        await Should.NotThrowAsync(async () => await _tripService.DeleteTripByIdAsync(trip.Id));

        await Should.ThrowAsync<ArgumentException>(async () => await _tripService.GetTripByIdAsync(trip.Id));
    }

    [Fact]
    public async Task DeleteTrip_NoTripExists_Fails()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await _tripService.DeleteTripByIdAsync(45));
    }

    [Fact]
    public async Task UserModifyAccessCheck_Valid_Success()
    {
        int userId = 100;
        Trip trip = await CreateTestTripWithUserId(userId);

        Assert.True(await _tripService.UserHasModifyAccessToTrip(userId, trip.Id));
    }

    [Fact]
    public async Task UserModifyAccessCheck_NotValid_Fail()
    {
        int userId = 100;
        Trip trip = await CreateTestTripWithUserId(userId);

        Assert.False(await _tripService.UserHasModifyAccessToTrip(userId + 1, trip.Id));
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
    public async Task UpdateTrip_TripDoesNotExist_ThrowsArgumentException()
    {
        var request = new UpdateTripRequest
        {
            Id = 999
        };

        await Should.ThrowAsync<ArgumentException>(
            () => _tripService.UpdateTripAsync(request, 1)
        );
    }

    [Fact]
    public async Task UpdateTrip_TripDoesNotExist_DoesNotCreateTrip()
    {
        await using var beforeContext =
            await _mockDbContextFactory.CreateDbContextAsync();

        var numberOfTripsBefore = await beforeContext.Set<Trip>().CountAsync();

        var request = new UpdateTripRequest
        {
            Id = 999,
            Title = "Updated trip"
        };

        await Should.ThrowAsync<ArgumentException>(
            () => _tripService.UpdateTripAsync(request, 1)
        );

        await using var afterContext =
            await _mockDbContextFactory.CreateDbContextAsync();

        var numberOfTripsAfter = await afterContext.Set<Trip>().CountAsync();

        numberOfTripsAfter.ShouldBe(numberOfTripsBefore);
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
            "Invalid date, date must be formatted"
        );

        var savedTrip = await _tripService.GetTripByIdAsync(trip.Id);

        savedTrip.Title.ShouldBe(originalTitle);
        savedTrip.Description.ShouldBe(originalDescription);
        savedTrip.StartDate.ShouldBe(originalStartDate);
        savedTrip.EndDate.ShouldBe(originalEndDate);
    }
}