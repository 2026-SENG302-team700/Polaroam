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

namespace SENG302.Api.Tests.Unit.Services;

public class TripServiceUnitTest : DatabaseUnitTestFixture
{
    private readonly NewTripRequest testTripRequest = new NewTripRequest
    {
        Title = "test trip", Description = "Description", StartDate = new DateTime(2021, 12, 29), EndDate = new DateTime(2022, 12, 31),
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
}