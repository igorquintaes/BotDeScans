using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace BotDeScans.App.Extensions;

public static class InteractionContextExtensions
{
    public static EmbedAuthor GetAuthor(this InteractionContext interactionContext) =>
        new(Name: interactionContext!.GetUserName(),
            IconUrl: interactionContext!.GetUserAvatarUrl());

    public static string GetUserName(this InteractionContext interactionContext) =>
        interactionContext.Interaction.Member.Value!.User.Value!.Username;

    private static Snowflake GetUserId(this InteractionContext interactionContext) =>
        interactionContext.Interaction.Member.Value!.User.Value!.ID;

    private static string? GetUserAvatar(this InteractionContext interactionContext) =>
        interactionContext.Interaction.Member.Value!.User.Value!.Avatar?.Value;

    private static Optional<string> GetUserAvatarUrl(this InteractionContext interactionContext)
    {
        var userId = GetUserId(interactionContext);
        var avatar = GetUserAvatar(interactionContext);

        return avatar is not null
            ? $"https://cdn.discordapp.com/avatars/{userId}/{avatar}.png"
            : new Optional<string>();
    }
}
