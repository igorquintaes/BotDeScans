using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Initializations.Setups;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddSetupDependencies(this IServiceCollection services) => services
        .AddSingleton<ISetup, DatabaseSetup>()
        .AddSingleton<ISetup, DiscordSlashCommandsSetup>()
        .AddSingleton<ISetup, DiscordValidatorSetup>()
        .AddSingleton<ISetup, StepsSetup>()
        .AddSingleton<ISetup, ClientsSetup>();
}
