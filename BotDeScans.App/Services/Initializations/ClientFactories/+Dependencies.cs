using BotDeScans.App.Extensions;
using Box.V2;
using CG.Web.MegaApiClient;
using Google.Apis.Blogger.v3;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Initializations.ClientFactories;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddClientFactories(this IServiceCollection services) => services
        .AddExternalClientAsSingleton<IBoxClient, BoxClientFactory>()
        .AddExternalClientAsSingleton<BloggerService, GoogleBloggerClientFactory>()
        .AddExternalClientAsSingleton<DriveService, GoogleDriveClientFactory>()
        .AddExternalClientAsScoped<IMegaApiClient, MegaClientFactory>()
        .AddExternalClientAsScoped<MangaDexAccessToken, MangaDexClientTokenFactory>();
}
