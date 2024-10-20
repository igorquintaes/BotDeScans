using BotDeScans.App.Services.Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Discord.Commands;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddDiscordCommands(this IServiceCollection services) => services
        .AddDiscordCommands(true)
        .AddCommandTree()
            .WithCommandGroup<GoogleDriveCommands>()
            .WithCommandGroup<PublishCommands>()
            .Finish();
}