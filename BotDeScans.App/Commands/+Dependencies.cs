using BotDeScans.App.Commands;
using BotDeScans.App.Commands.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Commands;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddDiscordCommands(this IServiceCollection services) => services
        .AddDiscordCommands(true)
        .AddDiscordInteractions()
        .AddCommandTree()
            .WithCommandGroup<GoogleDriveCommands>()
            .WithCommandGroup<PublishCommands>()
            .Finish();
}