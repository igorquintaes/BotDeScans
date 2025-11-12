using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.Initializations.Factories;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.GoogleDrive;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddGoogleDrive(this IServiceCollection services) => services
        .AddCommandTree()
            .WithCommandGroup<Commands>()
            .Finish()
        .AddScoped<GoogleDriveService>()
        .AddScoped<GoogleDriveFilesService>()
        .AddScoped<GoogleDriveFoldersService>()
        .AddScoped<GoogleDrivePermissionsService>()
        .AddScoped<GoogleDriveResourcesService>()
        .AddScoped<GoogleDriveSettingsService>()
        .AddScoped<GoogleDriveClientFactory>();
}
