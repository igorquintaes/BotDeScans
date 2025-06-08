using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Features.Publish.Command;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddCommands(
        this IServiceCollection services) =>
        services.AddCommandTree()
                .WithCommandGroup<Commands>()
                .Finish();
}
