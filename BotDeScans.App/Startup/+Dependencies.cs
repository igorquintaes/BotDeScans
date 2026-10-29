using BotDeScans.App.Startup.Setup;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddStartupDependencies(this IServiceCollection services) => services
        .AddSetupDependencies();
}
