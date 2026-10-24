using BotDeScans.App;
using BotDeScans.App.Infra;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace BotDeScans.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private WireMockServer wireMock = null!;

    public IHost Host { get; private set; } = null!;
    public WireMockServer WireMock => wireMock;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        wireMock = WireMockServer.Start();

        Environment.SetEnvironmentVariable("Discord__Token", "integration-test-token");
        Environment.SetEnvironmentVariable("Discord__ServerId", "1");
        Environment.SetEnvironmentVariable("Discord__ApplicationId", "1");

        Host = Program.CreateHostBuilder(
                [],
                enableDiscord: true,
                buildDiscordClient: client => client.ConfigureHttpClient(httpClient =>
                    httpClient.BaseAddress = new Uri($"{wireMock.Url}/api/v10/")))
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
        wireMock?.Stop();
        wireMock?.Dispose();
        await connection.DisposeAsync();
        Environment.SetEnvironmentVariable("Discord__Token", null);
        Environment.SetEnvironmentVariable("Discord__ServerId", null);
        Environment.SetEnvironmentVariable("Discord__ApplicationId", null);
    }
}
