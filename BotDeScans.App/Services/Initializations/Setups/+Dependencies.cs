using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Initializations.Setups;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddSetupDependencies(this IServiceCollection services) => services
        .AddSingleton<ISetup, DatabaseSetup>()
        .AddScoped<ISetup, DatabaseSetup>()
        .AddScoped<ISetup, DiscordSlashCommandsSetup>()
        .AddScoped<ISetup, DiscordValidatorSetup>()
        .AddScoped<ISetup, StepsSetup>()
        .AddScoped<ISetup, ClientsSetup>();
}
