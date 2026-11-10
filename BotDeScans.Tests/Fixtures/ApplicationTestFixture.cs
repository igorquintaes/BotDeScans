using BotDeScans.App;
using BotDeScans.App.Infra;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WireMock.Server;

namespace BotDeScans.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class ApplicationTestCollection : ICollectionFixture<ApplicationTestFixture>
{
    public const string Name = "Application tests";
}

public sealed class ApplicationTestFixture : IAsyncLifetime
{
    private SqliteConnection connection = null!;

    public IHost Host { get; private set; } = null!;
    public WireMockServer DiscordServer { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DiscordServer = WireMockServer.Start();

        Environment.SetEnvironmentVariable("Discord__Token", "integration-test-token");
        Environment.SetEnvironmentVariable("Discord__ServerId", "1");
        Environment.SetEnvironmentVariable("Discord__ApplicationId", "1");

        var discordFakeUri = new Uri($"{DiscordServer.Url}/api/v10/");

        void discordBuilder(IHttpClientBuilder client) => client
            .ConfigureHttpClient(httpClient => httpClient.BaseAddress = discordFakeUri);

        Host = Program
            .CreateHostBuilder([], buildDiscordClient: discordBuilder)
            .ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<DatabaseContext>();
                services.RemoveAll<DbContextOptions<DatabaseContext>>();
                services.AddDbContext<DatabaseContext>(options => options.UseSqlite(connection));
            })
            .Build();

        await Host.StartAsync();
        await using var scope = Host.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        await database.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Host is not null)
            await Host.StopAsync();

        Host?.Dispose();
        DiscordServer?.Stop();
        DiscordServer?.Dispose();
        await connection.DisposeAsync();
        Environment.SetEnvironmentVariable("Discord__Token", null);
        Environment.SetEnvironmentVariable("Discord__ServerId", null);
        Environment.SetEnvironmentVariable("Discord__ApplicationId", null);

        GC.SuppressFinalize(this);
    }
}
