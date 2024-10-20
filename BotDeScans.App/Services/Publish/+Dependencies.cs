using BotDeScans.App.Services.Publish.Steps;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Publish;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddPublishServices(this IServiceCollection services) => services
        .AddPublishSteps()
        .AddScoped<PublishService>()
        .AddScoped<PublishState>();
}
