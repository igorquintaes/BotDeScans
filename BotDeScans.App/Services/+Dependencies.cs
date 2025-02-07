using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Titles;
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
        .AddGoogleDrive()
        .AddMega()
        .AddTitleServices()
        .AddInitializators()
        .AddLoggingServices()
        .AddWrappers()
        .AddSingleton<ChartService>()
        .AddSingleton<FileService>()
        .AddSingleton<ImageService>()
        .AddSingleton<StepsService>()
        .AddScoped<BoxService>()
        .AddScoped<FileReleaseService>()
        .AddScoped<GoogleBloggerService>()
        .AddScoped<MangaDexService>()
        .AddHttpClient<SlimeReadService>()
            .ConfigureHttpClient(client => {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "pt-BR,pt;q=0.8,en-US;q=0.5,en;q=0.3");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("Host", "tipaeupapai.slimeread.com:8443");
                client.DefaultRequestHeaders.Add("Origin", "https://slimeread.com");
                client.DefaultRequestHeaders.Add("User-Agent", "BotDeScans/1.0.0");
                client.DefaultRequestHeaders.Add("Priority", "u=0");
                client.DefaultRequestHeaders.Add("requestId", "miueo");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
                client.DefaultRequestHeaders.Add("TE", "trailers");
            })
            .Services
        .AddValidatorsFromAssemblyContaining<Program>();
}