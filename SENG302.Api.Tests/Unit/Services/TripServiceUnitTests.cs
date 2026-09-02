
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
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
    public TripServiceUnitTest()
    {
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _tripService = new TripService(_mockDbContextFactory, _fakeTimeProvider, new UserService(_mockDbContextFactory, _fakeTimeProvider));
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
    public async Task DeleteTrip_NoTripExists_Fails()
    {
        await Should.ThrowAsync<ArgumentException>(async () => await _tripService.DeleteTripByIdAsync(45));
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
    
}