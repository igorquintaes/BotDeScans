using BotDeScans.App.Infra;
using BotDeScans.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotDeScans.IntegrationTests.Specs.Application;

[Collection(ApplicationTestCollection.Name)]
public abstract class ApplicationTest(ApplicationTestFixture fixture) :  IAsyncLifetime
{
    protected ApplicationTestFixture Fixture { get; } = fixture;
    protected IServiceScope Scope { get; private set; } = null!;
    protected DatabaseContext Database => Scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    public async ValueTask InitializeAsync()
    {
        Scope = Fixture.Host.Services.CreateScope();
        await Database.Database.EnsureDeletedAsync();
        await Database.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Database.Database.EnsureDeletedAsync();
        Scope.Dispose();

        GC.SuppressFinalize(this);
    }
}
