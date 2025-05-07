using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Services.Discord.Autocomplete;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.References.List;

[Group("references")]
public class Commands(
    Handler handler,
    IFeedbackService feedbackService) : CommandGroup
{
    [Command("list")]
    [RoleAuthorize("Staff")]
    [Description("Adiciona ou atualiza referências externas para a obra.")]
    public async Task<IResult> ListAsync(
        [AutocompleteProvider(AutocompleteTitles.Id)]
        [Description("Nome da obra")]
        string titleName)
    {
        var result = await handler.ExecuteAsync(titleName, CancellationToken);
        var embed = result.IsSuccess
            ? EmbedBuilder.CreateSuccessEmbed(string.Join(Environment.NewLine, result.Value))
            : EmbedBuilder.CreateErrorEmbed(result);

        return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }
}
