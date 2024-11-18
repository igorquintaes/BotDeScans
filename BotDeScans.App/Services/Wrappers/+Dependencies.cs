using BotDeScans.App.Features.GoogleDrive.InternalServices;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Wrappers;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddWrappers(this IServiceCollection services) => services
        .AddSingleton<GoogleDriveWrapper>()
        .AddSingleton<StreamWrapper>();
}
