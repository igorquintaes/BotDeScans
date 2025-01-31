using BotDeScans.App.Attributes;
using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using System.Drawing;
namespace BotDeScans.App.Services.Discord.Conditions;

// todo: parametrizar a partir de arquivo de configuration... ou banco de dados no futuro!!
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

        var guildMemberResult = await discordRestGuildAPI.GetGuildMemberAsync(
            interactionContext.Interaction.GuildID.Value,
            interactionContext.Interaction.Member.Value.User.Value.ID,
            ct);

        if (!guildMemberResult.IsDefined(out var guildMember))
            return Result.FromError(guildMemberResult.Error!);

        var guildRolesResult = await discordRestGuildAPI.GetGuildRolesAsync(
            interactionContext.Interaction.GuildID.Value,
            ct);

        if (!guildRolesResult.IsDefined(out var guildRoles))
            return Result.FromError(guildRolesResult.Error!);

        var hasRequiredRoleResult = rolesService.ContainsAtLeastOneOfExpectedRoles(
            attribute.RoleNames,
            guildRoles,
            guildMember.Roles);

        if (hasRequiredRoleResult.IsFailed)
        {
            var fullErrorMessage = string.Join(". ", hasRequiredRoleResult.Errors.Select(x => x.Message));
            return Result.FromError(new InvalidOperationError(fullErrorMessage));
        }

        if (hasRequiredRoleResult.Value is true)
            return Result.FromSuccess();

        var interactionResponseResult = await discordRestInteractionAPI.CreateInteractionResponseAsync(
             interactionContext.Interaction.ID,
             interactionContext.Interaction.Token,
             new InteractionResponse(
                 InteractionCallbackType.ChannelMessageWithSource,
                 new(new InteractionMessageCallbackData(Embeds: new[] {
                    new Embed(
                        Title: "Unauthorized!",
                        Description: $"You aren't in any of {string.Join(", ", attribute.RoleNames)} role(s)!",
                        Colour: Color.Red)
                 }))),
             ct: ct);

        return interactionResponseResult.IsSuccess
            ? new InvalidOperationError($"Invalid request for {interactionContext.Interaction.Member.Value.User.Value.ID} user. No role authorization for the user.")
            : interactionResponseResult;
    }
}
