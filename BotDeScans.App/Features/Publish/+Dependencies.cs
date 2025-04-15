using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Interactivity.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.Publish;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddPublishServices(
        this IServiceCollection services) =>
        services
            .AddCommandTree()
                .WithCommandGroup<PublishCommands>()
                .Finish()
            .AddInteractionGroup<PublishInteractions>()
            .AddPublishSteps()
            .AddPings()
            .AddScoped<PublishHandler>()
            .AddScoped<PublishQueries>()
            .AddScoped<PublishReplacerService>()
            .AddScoped<PublishService>()
            .AddScoped<PublishMessageService>()
            .AddScoped(serviceProvider => new PublishState
            {
                Steps = serviceProvider
                    .GetRequiredService<StepsService>()
                    .GetPublishSteps()
                    .Select(x => (x, StepStatus.Queued))
                    .ToArray()
            });
}
