using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.GoogleDrive;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddGoogleDriveInternalServices(this IServiceCollection services) => services
        .AddScoped<GoogleDriveFilesService>()
        .AddScoped<GoogleDriveFoldersService>()
        .AddScoped<GoogleDrivePermissionsService>()
        .AddScoped<GoogleDriveResourcesService>()
        .AddScoped<GoogleDriveSettingsService>();
}
