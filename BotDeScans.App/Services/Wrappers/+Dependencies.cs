using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Wrappers;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddWrappers(this IServiceCollection services) => services
        .AddSingleton<GoogleWrapper>()
        .AddSingleton<SlashServiceWrapper>()
        .AddSingleton<StreamWrapper>();
}
