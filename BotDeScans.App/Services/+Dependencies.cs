using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.Initializatiors;
using BotDeScans.App.Services.Logging;
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
        .AddExternalClients()
        .AddGoogleDriveInternalServices()
        .AddInitializators()
        .AddLoggingServices()
        .AddWrappers()
        .AddSingleton<ChartService>()
        .AddSingleton<ExtractionService>()
        .AddSingleton<FileService>()
        .AddSingleton<ImageService>()
        .AddSingleton<StepsService>()
        .AddScoped<BoxService>()
        .AddScoped<FileReleaseService>()
        .AddScoped<GoogleBloggerService>()
        .AddScoped<MangaDexService>()
        .AddScoped<MegaService>()
        .AddScoped<TsukiService>()
        .AddValidatorsFromAssemblyContaining<Program>();
}