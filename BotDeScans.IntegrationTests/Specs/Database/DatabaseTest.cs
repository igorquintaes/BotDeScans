using AutoFixture.AutoFakeItEasy;
using BotDeScans.App.Infra;
using BotDeScans.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BotDeScans.IntegrationTests.Specs.Database;

[Collection(DatabaseTestCollection.Name)]
public abstract class DatabaseTest(DatabaseTestFixture databaseTestFixture) : IAsyncLifetime
{
    protected readonly IFixture fixture = CreateFixture();
    protected DatabaseFacade Database => databaseTestFixture.Context.Database;
    protected DatabaseContext Context => databaseTestFixture.Context;
    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

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
    protected static IFixture CreateFixture()
    {
        var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
        fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        return fixture;
    }
}
