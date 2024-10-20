using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;
namespace BotDeScans.App.Services.Discord;

public class RolesService
{
    public virtual Result<bool> ContainsAtLeastOneOfExpectedRoles(
        IEnumerable<string> expectedRoles,
        IEnumerable<IRole> guildRoles,
        IEnumerable<Snowflake> userRoles)
    {
        var serverExpectedRoles = guildRoles
            .Where(guildRole => expectedRoles
            .Contains(guildRole.Name));

        if (!serverExpectedRoles.Any())
            return new NotFoundError($"Invalid request. No role(s) found in server; {string.Join(", ", expectedRoles)}");

        var hasRequiredRole = userRoles
            .Any(id => serverExpectedRoles
            .Any(requiredRole => requiredRole.ID == id));

        return Result<bool>.FromSuccess(hasRequiredRole);
    }
}
