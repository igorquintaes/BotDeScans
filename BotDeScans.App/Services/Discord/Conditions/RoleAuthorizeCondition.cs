using BotDeScans.App.Attributes;
using BotDeScans.App.Extensions;
using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System.Drawing;
namespace BotDeScans.App.Services.Discord.Conditions;

// todo: parametrizar a partir de arquivo de configuration
public class RoleAuthorizeCondition(
    IOperationContext commandContext,
    IDiscordRestGuildAPI discordRestGuildAPI,
    IDiscordRestInteractionAPI discordRestInteractionAPI,
    RolesService rolesService) : ICondition<RoleAuthorizeAttribute>
{
    public async ValueTask<Result> CheckAsync(
        RoleAuthorizeAttribute attribute,
        CancellationToken ct = default)
    {
        if (commandContext is not InteractionContext interactionContext)
            return new InvalidOperationError($"slash-command is mandatory!");

        var guildId = interactionContext.Interaction.GuildID.Value;
        var memberId = interactionContext.Interaction.Member.Value.User.Value.ID;
        var guildMemberResult = await discordRestGuildAPI.GetGuildMemberAsync(guildId, memberId, ct);
        if (!guildMemberResult.IsDefined(out var guildMember))
            return Result.FromError(guildMemberResult.Error!);

        var expectedRoles = await rolesService.GetRoleFromGuildAsync(attribute.RoleName, ct);
        if (expectedRoles.IsFailed)
            return Result.FromError(expectedRoles.Errors.ToDiscordError());

        if (guildMember.Roles.Any(expectedRoles.Value.ID.Equals))
            return Result.FromSuccess();

        var interactionResponseResult = await discordRestInteractionAPI.CreateInteractionResponseAsync(
             interactionContext.Interaction.ID,
             interactionContext.Interaction.Token,
             new InteractionResponse(
                 InteractionCallbackType.ChannelMessageWithSource,
                 new(new InteractionMessageCallbackData(Embeds: new[] {
                    new Embed(
                        Title: "Unauthorized!",
                        Description: $"You aren't in any of {string.Join(", ", attribute.RoleName)} role(s)!",
                        Colour: Color.Red)
                 }))),
             ct: ct);

        var userName = interactionContext.Interaction.Member.Value.User.Value.Username;
        return interactionResponseResult.IsSuccess
            ? new InvalidOperationError(
                $"Invalid request for user: {userName}, Id: {memberId}. " +
                $"No role authorization for the user.")
            : interactionResponseResult;
    }
}
