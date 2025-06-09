using Remora.Discord.API.Abstractions.Objects;

namespace BotDeScans.App.Services.Discord.Cache;

public class ScopedRoleCache
{
    public IReadOnlyList<IRole> Roles { get; set; } = [];

    public bool NeedsCache => Roles.Count == 0;
}