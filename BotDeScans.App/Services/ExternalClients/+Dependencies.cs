using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.ExternalClients;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddExternalClients(this IServiceCollection services) => services
        .AddSingleton<BloggerClient>()
        .AddSingleton<BoxClient>()
        .AddSingleton<GoogleDriveClient>()
        .AddSingleton<MegaClient>()
        // Bellow clients need to be initialized in every interaction
        // (External limitations)
        .AddHttpClient<TsukiClient>(client => TsukiClient.SetupClient(client)).Services;
}
