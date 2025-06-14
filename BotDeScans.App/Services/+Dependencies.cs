﻿using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.References;
using BotDeScans.App.Features.Titles;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Logging;
using BotDeScans.App.Services.MangaDex;
using BotDeScans.App.Services.Wrappers;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddServices(this IServiceCollection services) => services
        .AddDiscordServices()
        .AddPublishServices()
        .AddMangaDexServices()
        .AddGoogleDrive()
        .AddMega()
        .AddTitleServices()
        .AddReferencesServices()
        .AddInitializators()
        .AddLoggingServices()
        .AddWrappers()
        .AddClientFactories()
        .AddSingleton<ChartService>()
        .AddSingleton<FileService>()
        .AddSingleton<ImageService>()
        .AddScoped<BoxService>()
        .AddScoped<FileReleaseService>()
        .AddScoped<GoogleBloggerService>()
        .AddValidatorsFromAssemblyContaining<Program>();
}