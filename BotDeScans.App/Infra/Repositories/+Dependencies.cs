using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Infra.Repositories;

[ExcludeFromCodeCoverage]
internal static class Dependencies
{
    internal static IServiceCollection AddRepositories(this IServiceCollection services) =>
        services.AddScoped<TitleRepository>();
}
