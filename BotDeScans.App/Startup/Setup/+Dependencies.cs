using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Startup.Setup;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddSetupDependencies(this IServiceCollection services) => services
        .AddSingleton<ISetup, DatabaseSetup>()
        .AddSingleton<ISetup, DiscordSetup>()
        .AddSingleton<ISetup, StepsSetup>()
        .AddSingleton<ISetup, ClientsSetup>();
}
