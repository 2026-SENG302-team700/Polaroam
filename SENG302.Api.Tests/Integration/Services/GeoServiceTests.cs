using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Responses;
using SENG302.Api.Services;
using SENG302.Api.Tests.Integration;
using Shouldly;

namespace SENG302.Api.Tests.Integration.Services;

public class GeoServiceTests : BaseIntegrationTestFixture
{
    
    public GeoServiceTests(WebApplicationFactory<Program> webAppFactory) : base(webAppFactory) { }
    
    private sealed class TestHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory
    ) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            LastRequest = request;
            RequestCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responseFactory(request, cancellationToken));
        }
    }

    private static GeoLocationSearchResult CreateLocation(long osmId = 123)
    {
        return new GeoLocationSearchResult
        {
            OsmId = osmId,
            OsmType = "node",
            Latitude = "-43.5321",
            Longitude = "172.6362",
            Name = "Christchurch",
            DisplayName = "Christchurch, New Zealand"
        };
    }

    private static GeoService CreateService(TestHttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://nominatim.test/")
        };
        var dbContextFactory = Substitute.For<IDbContextFactory<DatabaseContext>>();
        return new GeoService(client, dbContextFactory);
    }

    [Fact]
    public async Task GetLocationsAsync_ValidResponse_ReturnsLocations()
    {
        var expectedLocation = CreateLocation();
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GeoLocationSearchResult> { expectedLocation })
            }
        );
        var service = CreateService(handler);

        var locations = await service.GetLocationsAsync(
            "Christchurch",
            "en-NZ",
            CancellationToken.None
        );

        locations.Count.ShouldBe(1);
        locations[0].OsmId.ShouldBe(expectedLocation.OsmId);
        locations[0].DisplayName.ShouldBe(expectedLocation.DisplayName);
    }

    [Fact]
    public async Task GetLocationsAsync_ValidQuery_AddsQueryToRequest()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GeoLocationSearchResult>())
            }
        );
        var service = CreateService(handler);

        await service.GetLocationsAsync(
            "New Zealand",
            null,
            CancellationToken.None
        );

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri.ShouldNotBeNull();
        handler.LastRequest.RequestUri.Query.ShouldContain("q=New%20Zealand");
        handler.LastRequest.RequestUri.Query.ShouldContain("format=json");
    }

    [Fact]
    public async Task GetLocationsAsync_AcceptLanguageProvided_AddsHeader()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GeoLocationSearchResult>())
            }
        );
        var service = CreateService(handler);

        await service.GetLocationsAsync(
            "Christchurch",
            "en-NZ",
            CancellationToken.None
        );

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Headers.GetValues("Accept-Language").Single().ShouldBe("en-NZ");
    }

    [Fact]
    public async Task GetLocationsAsync_AcceptLanguageNotProvided_DoesNotAddHeader()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GeoLocationSearchResult>())
            }
        );
        var service = CreateService(handler);

        await service.GetLocationsAsync(
            "Christchurch",
            null,
            CancellationToken.None
        );

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Headers.Contains("Accept-Language").ShouldBeFalse();
    }

    [Fact]
    public async Task GetLocationsAsync_NoResults_ReturnsEmptyList()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GeoLocationSearchResult>())
            }
        );
        var service = CreateService(handler);

        var locations = await service.GetLocationsAsync(
            "Not a place",
            null,
            CancellationToken.None
        );

        locations.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLocationsAsync_UnsuccessfulResponse_ThrowsHttpRequestException()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
        );
        var service = CreateService(handler);

        var exception = await Should.ThrowAsync<HttpRequestException>(() =>
            service.GetLocationsAsync(
                "Christchurch",
                null,
                CancellationToken.None
            )
        );

        exception.Message.ShouldContain("InternalServerError");
    }

    [Fact]
    public async Task GetLocationsAsync_CancelledRequest_ThrowsOperationCanceledException()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
        );
        var service = CreateService(handler);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.GetLocationsAsync(
                "Christchurch",
                null,
                cancellationTokenSource.Token
            )
        );
    }

    [Theory]
    [InlineData("node", "N")]
    [InlineData("way", "W")]
    [InlineData("relation", "R")]
    public async Task GetLocationAsync_ValidOsmType_UsesCorrectPrefix(
        string osmType,
        string expectedPrefix
    )
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GeoLocationSearchResult> { CreateLocation() })
            }
        );
        var service = CreateService(handler);

        await service.GetLocationAsync(
            osmId: 123, 
            osmType: osmType);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri.ShouldNotBeNull();
        handler.LastRequest.RequestUri.Query.ShouldContain($"osm_ids={expectedPrefix}123");
        handler.LastRequest.RequestUri.Query.ShouldContain("format=json");
    }

    [Fact]
    public async Task GetLocationAsync_ExistingLocation_ReturnsLocation()
    {
        var expectedLocation = CreateLocation();
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GeoLocationSearchResult> { expectedLocation })
            }
        );
        var service = CreateService(handler);

        var location = await service.GetLocationAsync(osmId: 123);

        location.ShouldNotBeNull();
        location.OsmId.ShouldBe(expectedLocation.OsmId);
        location.DisplayName.ShouldBe(expectedLocation.DisplayName);
    }

    [Fact]
    public async Task GetLocationAsync_LocationDoesNotExist_ReturnsNull()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<GeoLocationSearchResult>())
            }
        );
        var service = CreateService(handler);

        var location = await service.GetLocationAsync(999);

        location.ShouldBeNull();
    }

    [Fact]
    public async Task GetLocationAsync_InvalidOsmType_ThrowsArgumentException()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
        );
        var service = CreateService(handler);

        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            service.GetLocationAsync(123, osmType: "invalid")
        );

        exception.Message.ShouldContain("Invalid osmType");
        handler.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetLocationAsync_UnsuccessfulResponse_ThrowsHttpRequestException()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
        );
        var service = CreateService(handler);

        var exception = await Should.ThrowAsync<HttpRequestException>(() =>
            service.GetLocationAsync(999)
        );

        exception.Message.ShouldContain("NotFound");
    }
}
