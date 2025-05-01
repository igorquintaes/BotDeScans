using BotDeScans.App.Extensions;
using BotDeScans.App.Features.GoogleDrive.Discord;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.Initializations.Factories;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.GoogleDrive;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddGoogleDrive(this IServiceCollection services) => services
        .AddCommandTree()
            .WithCommandGroup<GoogleDriveCommands>()
            .Finish()
        .AddScoped<GoogleDriveService>()
        .AddScoped<GoogleDriveFilesService>()
        .AddScoped<GoogleDriveFoldersService>()
        .AddScoped<GoogleDrivePermissionsService>()
        .AddScoped<GoogleDriveResourcesService>()
        .AddScoped<GoogleDriveSettingsService>()
        .AddScoped<GoogleDriveClientFactory>();
}
