using BotDeScans.App.Infra;
using BotDeScans.Tests.Fixtures;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BotDeScans.Tests.Specs;

[Trait("Category", "Database")]
[Collection(DatabaseTestCollection.Name)]
public abstract class DatabaseTest(DatabaseTestFixture databaseTestFixture) : Test, IAsyncLifetime
{
    protected DatabaseContext Context => databaseTestFixture.Context;
    protected DatabaseFacade Database => databaseTestFixture.Context.Database;

    public async ValueTask InitializeAsync()
    {
        Context.ChangeTracker.Clear();

        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Database.EnsureDeletedAsync();

        GC.SuppressFinalize(this);
    }
}
