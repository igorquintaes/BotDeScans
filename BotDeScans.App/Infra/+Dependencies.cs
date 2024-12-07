using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Infra;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddInfraDependencies(this IServiceCollection services)
        => services.AddDbContext<DatabaseContext>();
}
