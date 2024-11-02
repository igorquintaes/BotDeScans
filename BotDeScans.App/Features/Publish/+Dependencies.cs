﻿using BotDeScans.App.Features.Publish.Steps;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.Publish;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddPublishServices(this IServiceCollection services) => services
        .AddPublishSteps()
        .AddScoped<PublishHandler>()
        .AddScoped<PublishService>()
        .AddScoped<PublishState>();
}