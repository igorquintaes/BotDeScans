using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Mega.Discord;
using BotDeScans.App.Features.Mega.InternalServices;
using CG.Web.MegaApiClient;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.Mega;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddMega(this IServiceCollection services) => services
        .AddCommandTree()
            .WithCommandGroup<MegaCommands>()
            .Finish()
        .AddScoped<MegaService>()
        .AddScoped<MegaFilesService>()
        .AddScoped<MegaFoldersService>()
        .AddScoped<MegaResourcesService>()
        .AddScoped<MegaSettingsService>()
        .AddExternalClientAsScoped<IMegaApiClient, MegaClientFactory>();
}
