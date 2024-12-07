using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Interactivity.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.Titles.Update;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddTitleUpdateServices(this IServiceCollection services) => services
        .AddInteractionGroup<UpdateInteractions>();
}