using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SENG302.Api.DataAccess;
using SENG302.Api.Services;
using Reqnroll.Assist;
using SENG302.Api.Services.Email;
using SENG302.Api.Services.ImageTagging;
using Microsoft.Extensions.Hosting;

namespace SENG302.Api.Tests.Integration;

public abstract class BaseIntegrationTestFixture : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _webAppFactory;

    // In-memory sqlite connections will get immediately closed if they aren't in use
    private readonly SqliteConnection _connection;

    protected IServiceProvider ServiceProvider => _webAppFactory.Server.Services.CreateScope().ServiceProvider;

    protected IDbContextFactory<DatabaseContext> DbContextFactory => ServiceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();

    protected IGeoService GeoServiceMock { get; }
    protected IImageTaggingQueue ImageTaggingQueueMock { get; }

    // As our tests run, time passes, which can make it difficult to check exactly when something happened.
    // For this reason, we fake (similar to mocking) our time provider, effectively freezing time while we run our tests.
    // Note: This will only work if we remember to use a TimeProvider instead of DateTimeOffset.UtcNow in our code.
    protected DateTimeOffset TestNow;
    protected readonly FakeTimeProvider FakeTimeProvider;
    protected string FakeTestDirectory; // Fake Test Directory for Unit Tests regarding FileService

    protected HttpClient HttpClient { get; private init; }


    protected BaseIntegrationTestFixture(WebApplicationFactory<Program> webAppFactory)
    {
        TestNow = DateTimeOffset.UtcNow;
        FakeTimeProvider = new FakeTimeProvider(TestNow);
        FakeTestDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        GeoServiceMock = Substitute.For<IGeoService>();
        ImageTaggingQueueMock = Substitute.For<IImageTaggingQueue>();
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _webAppFactory = webAppFactory.WithWebHostBuilder(config =>
        {
            // Changes we need to make to app configuration directly
            config.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {
                        // Mocking a file service path with a fake one to not build up unnecessary files.
                        "FileStorage:BasePath", FakeTestDirectory
                    }
                });
            });

            // There are some changes we need to make between how our app is configured normally, and how it should be configured for tests
            // In our tests, we want to mock / fake some things, or completely replace others
            config.ConfigureTestServices(services =>
            {
                // We want to use a dedicated in-memory database for tests so that we know exactly what should be in it
                // So, we first remove any database factories that are present
                services.RemoveAll<IDbContextFactory<DatabaseContext>>();
                // Then, we add a test-specific database factory, which will run everything in memory
                services.AddDbContextFactory<DatabaseContext>(dbConfig => dbConfig.UseSqlite(_connection));

                // Remove the existing time provider, which uses the real system time (and will change constantly through our tests)
                services.RemoveAll<TimeProvider>();
                // Replace it with our fake time provider, which keeps a consistent time throughout
                services.AddSingleton<TimeProvider>(FakeTimeProvider);
                services.AddSingleton<IEmailService>(Substitute.For<IEmailService>());

                services.RemoveAll<IGeoService>();
                services.AddSingleton(GeoServiceMock);

                services.RemoveAll<IImageTaggingQueue>();
                services.AddSingleton(ImageTaggingQueueMock);

                // replace the real image tagging background service with a mock
                // so that we don't actually try to tag images in our tests
                var imageTaggingWorker = services.FirstOrDefault(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType == typeof(ImageTaggingBackgroundService));

                if (imageTaggingWorker != null)
                {
                    services.Remove(imageTaggingWorker);
                }

                // Replace authentication with test auth and add the reset password authentication test scheme
                services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                ConfigureAdditionalServices(services);
            });
        });

        HttpClient = _webAppFactory.CreateClient();
    }

    // Used to add mocks to new services
    protected virtual void ConfigureAdditionalServices(IServiceCollection services)
    {
        // no mocked services by default
    }
    public virtual async Task InitializeAsync()
    {
        // At the start of a test, make sure the database is created fresh
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        CreateTestDirectory();
    }

    public virtual async Task DisposeAsync()
    {
        // Dispose the Http client and webfactory aswell
        HttpClient.Dispose();
        // Close and dispose the sqlite connection when tests are done
        await _connection.CloseAsync();
        await _connection.DisposeAsync();

        await _webAppFactory.DisposeAsync();

        DisposeDirectory();
    }

    // Creates test directory bucket
    private void CreateTestDirectory()
    {
        Directory.CreateDirectory(FakeTestDirectory);
    }

    // Disposes test directory bucket at the end of test.
    private void DisposeDirectory()
    {
        if (Directory.Exists(FakeTestDirectory))
        {
            Directory.Delete(FakeTestDirectory, true);
        }
    }
}

