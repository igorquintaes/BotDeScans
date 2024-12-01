﻿using BotDeScans.App.Features.Titles.Create;
using BotDeScans.App.Features.Titles.Update;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.Titles;

[ExcludeFromCodeCoverage]
internal static class AddDependencies
{
    internal static IServiceCollection AddTitleServices(this IServiceCollection services) => services
        .AddCommandTree()
            .WithCommandGroup<TitleCommands>()
            .Finish()
        .AddAutocompleteProvider<AutocompleteTitleRoles>()
        .AddTitleCreateServices()
        .AddTitleUpdateServices();
}