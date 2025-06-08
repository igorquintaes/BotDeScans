using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddPings(this IServiceCollection services) => services
        .AddSingleton<Ping, EveryonePing>()
        .AddSingleton<Ping, NonePing>()
        .AddScoped<Ping, GlobalPing>()
        .AddScoped<Ping, RolePing>();
}
