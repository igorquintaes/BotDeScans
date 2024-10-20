using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.Commands.Extensions;
using Serilog;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Logging;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddLoggingServices(this IServiceCollection services) => services
        .AddPostExecutionEvent<LogEvent>()
        .AddSingleton<LoggerService>()
        .AddLogging();

    internal static IHostBuilder AddLoggingToHost(this IHostBuilder hostBuilder) => hostBuilder
        .UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("./log/log.txt", rollingInterval: RollingInterval.Day))
        .ConfigureLogging(logginBuilder => logginBuilder
            .AddConsole()
            .AddFilter("*", LogLevel.Information)
            .AddFilter("System.Net.Http.HttpClient.*.LogicalHandler", LogLevel.Warning)
            .AddFilter("System.Net.Http.HttpClient.*.ClientHandler", LogLevel.Warning));
}