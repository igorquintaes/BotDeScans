﻿using BotDeScans.App.Features.GoogleDrive.Discord;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.GoogleDrive;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddGoogleDriveInternalServices(this IServiceCollection services) => services
        .AddCommandTree()
            .WithCommandGroup<GoogleDriveCommands>()
            .Finish()
        .AddScoped<GoogleDriveService>()
        .AddScoped<GoogleDriveFilesService>()
        .AddScoped<GoogleDriveFoldersService>()
        .AddScoped<GoogleDrivePermissionsService>()
        .AddScoped<GoogleDriveResourcesService>()
        .AddScoped<GoogleDriveSettingsService>()
        .AddSingleton<ValidatorService>();
}
