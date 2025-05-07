using BotDeScans.App.Services.MangaDex.InternalServices;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.MangaDex;
[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddMangaDexServices(this IServiceCollection services) =>
        services.AddScoped<MangaDexService>()
                .AddScoped<MangaDexUploadService>();
}

