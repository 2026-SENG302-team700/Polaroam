using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SENG302.Api.Services;

namespace SENG302.Api.Tests.Integration.Services;

public class ScrapboardServiceTests : BaseIntegrationTestFixture
{
    private IScrapboardService ServiceUnderTest => ServiceProvider.GetRequiredService<IScrapboardService>();
    private readonly ITripService _tripService;
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly IUserService _mockUserService;
    public ScrapboardServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory)
    {
        _mockUserService = Substitute.For<IUserService>();
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _tripService = new TripService(DbContextFactory, _fakeTimeProvider, _mockUserService);
    }



    [Fact]
    private async Task CreateScrapboard_TripHasNoScrapboard_Success()
    {

    }

}