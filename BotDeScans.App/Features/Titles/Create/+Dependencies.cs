using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Interactivity.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.Titles.Create;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddTitleCreateServices(this IServiceCollection services) => services
        .AddInteractionGroup<CreateInteractions>();
}