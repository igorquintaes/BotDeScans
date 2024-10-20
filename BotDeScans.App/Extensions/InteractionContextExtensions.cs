using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace BotDeScans.App.Extensions;

public static class InteractionContextExtensions
{
    public static Optional<string> GetUserAvatarUrl(this InteractionContext interactionContext)
    {
        var userId = interactionContext.Interaction.Member.Value?.User.Value?.ID.Value;
        var avatar = interactionContext.Interaction.Member.Value?.User.Value?.Avatar?.Value;

        return userId is not null && avatar is not null
            ? $"https://cdn.discordapp.com/avatars/{userId}/{avatar}.png"
            : new Optional<string>();
    }
    public static string GetUserName(this InteractionContext interactionContext) 
        => interactionContext.Interaction.Member.Value?.User.Value?.Username ?? "Desconhecido";
}
