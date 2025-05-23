﻿using BotDeScans.App.Features.Publish.Discord;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Pings;

public class EveryonePing(IConfiguration configuration) : Ping(configuration)
{
    protected override PingType Type => PingType.Everyone;

    public override Task<string> GetPingAsTextAsync(CancellationToken cancellationToken) =>
        Task.FromResult("@everyone");
}
