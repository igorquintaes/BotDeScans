using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace BotDeScans.App.Extensions;

public static class InteractionContextExtensions
{
    public static Optional<string> GetUserAvatarUrl(this InteractionContext interactionContext)
    {
        var userId = GetUserId(interactionContext);
        var avatar = GetUserAvatar(interactionContext);

        return avatar is not null
            ? $"https://cdn.discordapp.com/avatars/{userId}/{avatar}.png"
            : new Optional<string>();
    }

    public static Snowflake GetUserId(this InteractionContext interactionContext) =>
        interactionContext.Interaction.Member.Value!.User.Value!.ID;

    public static string GetUserName(this InteractionContext interactionContext) =>
        interactionContext.Interaction.Member.Value!.User.Value!.Username ?? "Desconhecido";

    public static string? GetUserAvatar(this InteractionContext interactionContext) =>
        interactionContext.Interaction.Member.Value!.User.Value!.Avatar?.Value;
}
