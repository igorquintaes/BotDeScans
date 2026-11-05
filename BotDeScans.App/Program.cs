using BotDeScans.App.Infra;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Services.Logging;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.Interactivity.Extensions;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App;

[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Starting...");

        try
        {
            Result.Setup(cfg =>
            {
                cfg.Logger = new ResultLogger(Log.Logger);
                cfg.ExceptionalErrorFactory = (message, ex) =>
                {
                    var error = new Error(message).CausedBy(ex);
                    var errorResult = new Result().WithError(error);
                    cfg.Logger.Log(ex.Source, message, errorResult, LogLevel.Error);

                    return new ExceptionalError(message, ex);
                };
            });

            await CreateHostBuilder(args).Build().RunAsync();
            return Environment.ExitCode;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args,
        Action<IHttpClientBuilder>? buildDiscordClient = null) => Host
            .CreateDefaultBuilder(args)
            .AddDiscordToHost(buildDiscordClient)
            .AddLoggingToHost()
            .ConfigureServices(services => services
                .AddServices()
                .AddInfraDependencies()
                .AddLazyCache()
                .AddMangaDex()
                .AddMangaDexUtils()
                .AddInteractivity()
                    .AddHostedService<SetupApplication>())
            .ConfigureAppConfiguration(config => config
                .AddJsonFile("config.json", optional: true, reloadOnChange: true)
                .AddJsonFile("config.local.json", optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine("config", "config.json"), optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine("config", "config.local.json"), optional: true, reloadOnChange: true)
                .AddEnvironmentVariables())
            .UseConsoleLifetime();
}
