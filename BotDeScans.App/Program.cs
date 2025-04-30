using BotDeScans.App.Infra;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Initializatiors;
using BotDeScans.App.Services.Logging;
using FluentResults;
using MangaDexSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Interactivity.Extensions;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
namespace BotDeScans.App;

[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task Main(string[] args)
    {
        var cancelationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, @event) =>
        {
            Console.WriteLine("Closing...");
            cancelationTokenSource.Cancel();
            @event.Cancel = true;
        };

        var host = Host
            .CreateDefaultBuilder(args)
            .AddDiscordToHost()
            .AddLoggingToHost()
            .ConfigureServices(services => services
                .AddServices()
                .AddInfraDependencies()
                .AddDiscordCommands(true)
                .AddInteractivity()
                .AddLazyCache()
                .AddMangaDex())
            .ConfigureAppConfiguration(config => config
                .AddEnvironmentVariables()
                .AddJsonFile("config.json", optional: true, reloadOnChange: true)
                .AddJsonFile("config.local.json", optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine("config", "config.json"), optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine("config", "config.local.json"), optional: true, reloadOnChange: true))
            .UseConsoleLifetime()
            .Build();

        var warmupResult = Result.Ok();
        using (var scope = host.Services.CreateScope())
        {
            // todo
            //if (File.Exists(DatabaseContext.DbPath) is false)
            //    File.WriteAllBytes(DatabaseContext.DbPath, []);

            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await db.Database.MigrateAsync(cancelationTokenSource.Token);

            var setupDiscordService = scope.ServiceProvider.GetRequiredService<SetupDiscordService>();
            var discordUpdateResult = await setupDiscordService.SetupAsync(cancelationTokenSource.Token);
            warmupResult.WithReasons(discordUpdateResult.Reasons);

            var setupClientsService = scope.ServiceProvider.GetRequiredService<SetupStepsService>();
            var setupClientsResult = setupClientsService.Setup();
            warmupResult.WithReasons(setupClientsResult.Reasons);

            var setupPublishStepsService = scope.ServiceProvider.GetRequiredService<SetupClientsService>();
            var setupPublishStepsResult = await setupPublishStepsService.SetupAsync(cancelationTokenSource.Token);
            warmupResult.WithReasons(setupPublishStepsResult.Reasons);
        }

        if (warmupResult.IsFailed)
        {
            LogErrors(warmupResult);
            return;
        }

        await host.RunAsync();
    }

    private static void LogErrors(Result result)
    {
        var errorAsJson = JsonSerializer.Serialize(result.Errors);
        var defaultConsoleForegroundColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(errorAsJson);
        Log.Error(errorAsJson);

        Console.ForegroundColor = defaultConsoleForegroundColor;
        Console.ReadKey();
    }
}
