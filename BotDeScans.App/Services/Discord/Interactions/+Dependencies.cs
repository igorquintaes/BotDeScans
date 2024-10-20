using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Interactivity.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Discord.Interactions;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddDiscordInteractions(this IServiceCollection services) => services
        .AddInteractivity()
        .AddInteractionGroup<PublishInteractions>();
}