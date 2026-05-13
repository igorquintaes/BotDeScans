using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Interactivity.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Features.Publish.Interaction;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddInteractions(
        this IServiceCollection services) =>
        services
            .AddInteractionGroup<Interactions>()
            .AddPublishSteps()
            .AddPings()
            .AddScoped<Handler>()
            .AddScoped<SetupService>()
            .AddScoped<TextReplacer>()
            .AddScoped<DiscordPublisher>()
            .AddScoped<State>()
            .AddScoped<IPublishContext>(sp => sp.GetRequiredService<State>());
}
