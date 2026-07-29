using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.Routing.Handlers;
using SENG302.Api.DataAccess;

namespace SENG302.Api.Tests.Unit;

public abstract class DatabaseUnitTestFixture
{

    protected readonly IDbContextFactory<DatabaseContext> _mockDbContextFactory;

    // In-memory sqlite connections will get immediately closed if they aren't in use
    private readonly SqliteConnection _connection;

    private readonly DbContextOptions _options;
    
    protected DatabaseUnitTestFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<DatabaseContext>().UseSqlite(_connection).Options;

        _mockDbContextFactory = Substitute.For<IDbContextFactory<DatabaseContext>>();
        // chatgpt GPT-5.5 prompt: "With an NSubstitute returns(), how can I return a new copy of an object every time"
        _mockDbContextFactory.CreateDbContextAsync().Returns(_ => CreateInMemoryDatabaseContext());
        _mockDbContextFactory.CreateDbContext().Returns(_ => CreateInMemoryDatabaseContext());
    }

    /// <summary>
    /// Generates a new database context using the sqllite connection created in the constructor
    /// Ensures that the database is created
    /// </summary>
    /// <returns>
    /// Returns a new database context
    /// </returns>
    private DatabaseContext CreateInMemoryDatabaseContext()
    {
        DatabaseContext mockDatabaseContext = new DatabaseContext(_options);
        // chatgpt GPT-5.5 prompt: "<code pasted in> Is there anything horribly wrong with this?"
        mockDatabaseContext.Database.EnsureCreated();

        return mockDatabaseContext;
    }
}

