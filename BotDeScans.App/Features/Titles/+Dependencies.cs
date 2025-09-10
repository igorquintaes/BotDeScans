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
            .WithCommandGroup<SkipSteps.Commands>()
            .Finish()
        .AddInteractionGroup<Create.Interactions>()
        .AddInteractionGroup<Update.Interactions>()
        .AddScoped<Create.Handler>()
        .AddScoped<Update.Handler>()
        .AddScoped<SkipSteps.Add.Handler>()
        .AddScoped<SkipSteps.Remove.Handler>()
        .AddScoped<SkipSteps.List.Handler>()
        .AddScoped<Update.Persistence>()
        .AddScoped<SkipSteps.Add.Persistence>()
        .AddScoped<SkipSteps.Remove.Persistence>()
        .AddScoped<SkipSteps.List.Persistence>();
}