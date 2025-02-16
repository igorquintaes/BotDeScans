using Remora.Discord.API.Abstractions.Objects;

namespace BotDeScans.App.Extensions;

public static class RoleExtensions
{
    public static string ToDiscordString(this IRole role) => 
        $"<@&{role.ID.Value}>";
}
