using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Features.Titles.SkipSteps;

[ExcludeFromCodeCoverage]
internal static class Dependencies
{
    internal static IServiceCollection AddSkipSteps(this IServiceCollection services) => services
        .AddCommandTree()
            .WithCommandGroup<Commands>()
            .Finish()
        .AddScoped<Add.Handler>()
        .AddScoped<List.Handler>()
        .AddScoped<Remove.Handler>();
}
