using BotDeScans.App.Infra;
using BotDeScans.Tests.Fixtures;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;

namespace BotDeScans.Tests.Specs;

[Trait("Category", "Application")]
[Collection(ApplicationTestCollection.Name)]
public abstract class ApplicationTest(ApplicationTestFixture fixture) : Test, IAsyncLifetime
{
    protected WireMockServer DiscordServer = fixture.DiscordServer;
    protected IServiceScope Scope { get; private set; } = fixture.Host.Services.CreateScope()!;
    protected DatabaseContext Context => Scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    protected DatabaseFacade Database => Context.Database;

    public async ValueTask InitializeAsync()
    {
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();

        DiscordServer.Reset();
    }

    public async ValueTask DisposeAsync()
    {
        await Database.EnsureDeletedAsync();
        Scope.Dispose();

        GC.SuppressFinalize(this);
    }
}
