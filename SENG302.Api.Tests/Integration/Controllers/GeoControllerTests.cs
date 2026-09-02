using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using SENG302.Api.Controllers;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using Shouldly;

namespace SENG302.Api.Tests.Integration.ControllerTests;

public class GeoControllerTests : BaseIntegrationTestFixture
{
    private readonly IGeoService _mockGeoService;
    private readonly GeoController _controller;

    public GeoControllerTests(WebApplicationFactory<Program> webApplicationFactory) : base(webApplicationFactory)
    {
        _mockGeoService = Substitute.For<IGeoService>();
        _controller = new GeoController(_mockGeoService);
    }

    [Fact]
    public async Task SearchLocationsAsync_ValidQuery_ReturnsLocations()
    {
        var cancellationToken = new CancellationToken();
        var expectedLocations = new
        List<GeoLocationSearchResult>
          {
              new() {
                  OsmId = 123,
                  Latitude = "-43.5321",
                  Longitude = "172.6362",
                  Name = "Christchurch",
                  DisplayName = "Christchurch, New Zealand"
              }
          };

        // mock service
        _mockGeoService.GetLocationsAsync("Christchurch", "en-NZ", cancellationToken).Returns(expectedLocations);

        var result = await _controller.SearchLocationsAsync("Christchurch", "en-NZ", cancellationToken);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expectedLocations);

        await _mockGeoService.Received(1).GetLocationsAsync("Christchurch", "en-NZ", cancellationToken);
    }

    [Fact]
    public async Task SearchLocationsAsync_NoResults_ReturnsEmptyList()
    {
        var cancellationToken = new CancellationToken();
        var expectedLocations = new
        List<GeoLocationSearchResult>();

        _mockGeoService
            .GetLocationsAsync("Not a place", null, cancellationToken)
            .Returns(expectedLocations);

        var result = await
        _controller.SearchLocationsAsync("Not a place", null, cancellationToken);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var locations = okResult.Value.ShouldBeOfType<List<GeoLocationSearchResult>>();
        // check that there were no results returned
        locations.ShouldBeEmpty();

        await _mockGeoService.Received(1).GetLocationsAsync("Not a place", null, cancellationToken);
    }

    [Fact]
    public async Task GetLocationAsync_ExistingLocation_ReturnsLocation()
    {
        var expectedLocation = new
        GeoLocationSearchResult
        {
            OsmId = 123,
            Latitude = "-43.5321",
            Longitude = "172.6362",
            Name = "Christchurch",
            DisplayName = "Christchurch, New Zealand"
        };

        _mockGeoService
            .GetLocationAsync(123)
            .Returns(expectedLocation);

        var result = await _controller.GetLocationAsync(123);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expectedLocation);

        await _mockGeoService.Received(1).GetLocationAsync(123);
    }

    [Fact]
    public async Task GetLocationAsync_LocationDoesNotExist_ReturnsOkWithNull()
    {
        _mockGeoService
            .GetLocationAsync(999)
            .Returns((GeoLocationSearchResult?)null);

        var result = await _controller.GetLocationAsync(999);

        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBeNull();

        await _mockGeoService.Received(1).GetLocationAsync(999);
    }
}