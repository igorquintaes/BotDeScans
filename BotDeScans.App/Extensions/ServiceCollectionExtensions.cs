using BotDeScans.App.Services;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalClientAsSingleton<TClient, TFactory>(
        this IServiceCollection services)
        where TClient : class
        where TFactory : ClientFactory<TClient> => 
        services
            .AddScoped<TFactory>()
            .AddSingleton(GetExternalClientFromProvider<TClient, TFactory>);

    public static IServiceCollection AddExternalClientAsScoped<TClient, TFactory>(
        this IServiceCollection services)
        where TClient : class
        where TFactory : ClientFactory<TClient> => 
        services
            .AddScoped<TFactory>()
            .AddScoped(GetExternalClientFromProvider<TClient, TFactory>);

    private static TClient GetExternalClientFromProvider<TClient, TFactory>(
        IServiceProvider serviceProvider)
        where TClient : class
        where TFactory : ClientFactory<TClient>
    {
        var factory = serviceProvider.GetRequiredService<TFactory>();

        return factory.ExpectedInPublishFeature 
             ? factory.SafeCreateAsync(CancellationToken.None)
                      .GetAwaiter()
                      .GetResult()
                      .Value
             : null!;
    }
}
