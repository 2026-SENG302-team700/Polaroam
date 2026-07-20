using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Services;

public class TripServiceUnitTest : BaseUnitTestFixture
{
    private ITripService UserServiceUnderTest => ServiceProvider.GetRequiredService<ITripService>();
    private IDbContextFactory<DatabaseContext> DbFactory => ServiceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
    public TripServiceUnitTest(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory) { }

    [Fact]
    public async Task CreateNewTrip_UserDoesNotExist_ThrowsArgumentException()
    {
        var tripRequest = new NewTripRequest
        {
            Title = "test trip", Description = "Description", StartDate = new DateTime(2021, 12, 29), EndDate = new DateTime(2022, 12, 31),
        };

        await Should.ThrowAsync<ArgumentException>(async () => await UserServiceUnderTest.CreateNewTripAsync(tripRequest, -1));
    }
}