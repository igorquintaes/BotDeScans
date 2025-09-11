using BotDeScans.App.Features.Titles.SkipSteps;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Interactivity.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Features.Titles;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddTitleServices(this IServiceCollection services) => services
        .AddCommandTree()
            .WithCommandGroup<Create.Commands>()
            .WithCommandGroup<List.Commands>()
            .WithCommandGroup<Update.Commands>()
            .Finish()
        .AddInteractionGroup<Create.Interactions>()
        .AddInteractionGroup<Update.Interactions>()
        .AddScoped<Create.Handler>()
        .AddScoped<Update.Handler>()
        .AddScoped<Update.Persistence>()
        .AddSkipSteps();
}
