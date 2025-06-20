﻿using BotDeScans.App.Services.Initializations.Factories.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Extensions;

[ExcludeFromCodeCoverage]
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

        return factory.Enabled
             ? factory.SafeCreateAsync(CancellationToken.None)
                      .GetAwaiter()
                      .GetResult()
                      .Value
             : null!;
    }
}
