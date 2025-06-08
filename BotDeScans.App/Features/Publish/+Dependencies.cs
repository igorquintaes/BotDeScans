using BotDeScans.App.Features.Publish.Command;
using BotDeScans.App.Features.Publish.Interaction;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.Publish;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddPublishServices(
        this IServiceCollection services) =>
        services.AddCommands()
                .AddInteractions();
}
