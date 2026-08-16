using BotDeScans.App.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BotDeScans.IntegrationTests;

public abstract class IntegrationTest(IntegrationTestFixture fixture) : IAsyncLifetime
{
    protected IntegrationTestFixture Fixture { get; } = fixture;
    protected IServiceScope Scope { get; private set; } = null!;
    protected DatabaseContext Database => Scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    public async ValueTask InitializeAsync()
    {
        Scope = Fixture.Host.Services.CreateScope();
        await Database.Database.EnsureDeletedAsync();
        await Database.Database.EnsureCreatedAsync();
    }

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration tests";
}

    public async ValueTask DisposeAsync()
    {
        await Database.Database.EnsureDeletedAsync();
        Scope.Dispose();
    }
}
