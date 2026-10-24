using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Discord.Autocomplete;
using BotDeScans.App.Services.Discord.Cache;
using BotDeScans.App.Services.Discord.Conditions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Hosting.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
namespace BotDeScans.App.Services.Discord;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddDiscordServices(this IServiceCollection services) => services
        .AddDiscordCommands(true)
        .AddAutocompleteProvider<AutocompleteTitles>()
        .AddAutocompleteProvider<AutocompleteStepNames>()
        .AddCondition<RoleAuthorizeCondition>()
        .AddScoped<RolesService>()
        .AddScoped<ScopedRoleCache>()
        .AddScoped<ExtendedFeedbackService>()
        .Configure<JsonSerializerOptions>("Discord", options =>
            options.Converters.Insert(0, new NullSafeHeartbeatConverter()));

    internal static IHostBuilder AddDiscordToHost(
        this IHostBuilder hostBuilder,
        Action<IHttpClientBuilder>? buildClient = null) => hostBuilder
        .AddDiscordService(
            services => services
                .GetRequiredService<IConfiguration>().GetRequiredValue<string>("Discord:Token"),
            buildClient ?? (_ => { }));
}
