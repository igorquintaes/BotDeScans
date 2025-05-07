using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.References;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddReferencesServices(this IServiceCollection services) => services
        .AddCommandTree()
            .WithCommandGroup<List.Commands>()
            .WithCommandGroup<Update.Commands>()
            .Finish()
        .AddScoped<List.Handler>()
        .AddScoped<Update.Handler>();
}