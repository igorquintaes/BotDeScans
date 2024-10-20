using BotDeScans.App.Services;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Logging;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App;

[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task Main(string[] args) => await Host
        .CreateDefaultBuilder(args)
        .AddDiscordToHost()
        .AddLoggingToHost()
        .ConfigureServices(services => services
            .AddServices()
            .AddLazyCache()
            .AddMangaDex())
        .ConfigureAppConfiguration(config => config
            .AddEnvironmentVariables()
            .AddJsonFile("config.json", optional: true)
            .AddJsonFile(".\\config\\config.json", optional: true)
            .AddJsonFile("config.local.json", optional: true)
            .AddJsonFile(".\\config\\config.local.json", optional: true))
        .UseConsoleLifetime()
        .Build()
        .RunAsync();
}
