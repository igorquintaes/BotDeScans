using BotDeScans.App.Services.CapivaraTradutor.InternalServices;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.CapivaraTradutor;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddCapivaraTradutorServices(this IServiceCollection services) =>
        services.AddScoped<CapivaraTradutorService>()
                .AddSingleton<CapivaraTradutorUploadService>();
}
