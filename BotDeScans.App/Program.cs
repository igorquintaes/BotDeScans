using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Logging;
using BotDeScans.App.Warmup;
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
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, @event) =>
        {
            Console.WriteLine("Closing...");
            cts.Cancel();
            @event.Cancel = true;
        };

        Console.WriteLine("Starting...");

        var host = CreateHostBuilder(args).Build();

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

        var setupResult = await Setup.Run(host.Services, cts.Token);
        LogSetup(setupResult);

        if (setupResult.IsFailed)
        {
            return 1;
        }

        await host.RunAsync();
        return 0;
    }

    public static IHostBuilder CreateHostBuilder(
        string[] args,
        bool enableDiscord = true,
        Action<IHttpClientBuilder>? buildDiscordClient = null)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args);
        if (enableDiscord)
            hostBuilder.AddDiscordToHost(buildDiscordClient);

        return hostBuilder
            .AddLoggingToHost()
            .ConfigureServices(services =>
            {
                services
                    .AddServices(enableDiscord)
                    .AddInfraDependencies()
                    .AddLazyCache()
                    .AddMangaDex()
                    .AddMangaDexUtils();

                if (enableDiscord)
                    services.AddInteractivity();
            })
            .ConfigureAppConfiguration(config => config
                .AddJsonFile("config.json", optional: true, reloadOnChange: true)
                .AddJsonFile("config.local.json", optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine("config", "config.json"), optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine("config", "config.local.json"), optional: true, reloadOnChange: true)
                .AddEnvironmentVariables())
            .UseConsoleLifetime();
    }

    private static void LogSetup(Result result)
    {
        LogSuccesses(result);
        LogErrors(result);
    }

    private static void LogSuccesses(Result result)
    {
        var successesInfo = result.Successes.GetSuccessesInfo().Select(x => x.ToString());
        var SuccessesText = string.Join(Environment.NewLine, successesInfo);
        Log.Information(SuccessesText);

        var defaultConsoleForegroundColor = Console.ForegroundColor;
        var defaultBackgroundForegroundColor = Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(SuccessesText);

        Console.ForegroundColor = defaultConsoleForegroundColor;
        Console.WriteLine();
    }

    private static void LogErrors(Result result)
    {
        if (result.IsSuccess)
            return;

        var errorsInfo = result.Errors.GetErrorsInfo().Select(x => x.ToString());
        var errorsText = string.Join(Environment.NewLine, errorsInfo);
        Log.Error(errorsText);

        var defaultConsoleForegroundColor = Console.ForegroundColor;
        var defaultBackgroundForegroundColor = Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.BackgroundColor = ConsoleColor.Red;

        Console.WriteLine(errorsText);

        Console.ForegroundColor = defaultConsoleForegroundColor;
        Console.BackgroundColor = defaultBackgroundForegroundColor;
        Console.ReadLine();
    }
}
