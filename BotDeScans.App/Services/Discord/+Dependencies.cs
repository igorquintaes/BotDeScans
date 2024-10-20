using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord.Commands;
using BotDeScans.App.Services.Discord.Conditions;
using BotDeScans.App.Services.Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Hosting.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Discord;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddDiscordServices(this IServiceCollection services) => services
        .AddDiscordCommands()
        .AddDiscordInteractions()
        .AddDiscordConditions()
        .AddSingleton<RolesService>()
        .AddScoped<ExtendedFeedbackService>();

    internal static IHostBuilder AddDiscordToHost(this IHostBuilder hostBuilder) => hostBuilder
        .AddDiscordService(services => services
        .GetRequiredService<IConfiguration>().GetRequiredValue<string>("Discord:Token"));
}
