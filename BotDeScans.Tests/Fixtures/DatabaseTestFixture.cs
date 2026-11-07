using BotDeScans.App.Infra;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class DatabaseTestCollection : ICollectionFixture<DatabaseTestFixture>
{
    public const string Name = "Database tests";
}

public sealed class DatabaseTestFixture : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    public DatabaseContext Context = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        Context = new DatabaseContext(
            new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(connection)
            .Options);

        Context.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
