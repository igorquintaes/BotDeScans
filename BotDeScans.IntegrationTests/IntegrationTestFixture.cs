using BotDeScans.App;
using BotDeScans.App.Infra;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BotDeScans.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private SqliteConnection connection = null!;

    public IHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        Host = Program.CreateHostBuilder([], enableDiscord: false)
            .ConfigureServices(services =>
            {
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
        await connection.DisposeAsync();
    }
}
