﻿using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Pings;

public abstract class Ping(IConfiguration configuration)
{
    protected IConfiguration configuration = configuration;

    protected abstract PingType Type { get; }

    public const string PING_TYPE_KEY = "Settings:Publish:PingType";

    public virtual bool IsApplicable => configuration.GetValue(PING_TYPE_KEY, PingType.None) == Type;

    public abstract Task<string> GetPingAsTextAsync(CancellationToken cancellationToken);
}
