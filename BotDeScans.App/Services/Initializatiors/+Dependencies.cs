using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Initializatiors;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddInitializators(this IServiceCollection services) => services
        .AddScoped<SetupDiscordService>()
        .AddScoped<SetupStepsService>()
        .AddScoped<SetupClientsService>();
}
