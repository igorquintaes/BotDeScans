using BotDeScans.App.Services.Initializations.Setups;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Initializations;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddInitializators(this IServiceCollection services) => services
        .AddSetupDependencies()
        .AddSingleton<GoogleDriveInitializer>()
        .AddSingleton<SetupApplication>();
}
