using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord.Autocomplete;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.References.Update;

[Group("references")]
public class Commands(
    Handler handler,
    IFeedbackService feedbackService) : CommandGroup
{
    [Command("update")]
    [RoleAuthorize("Publisher")]
    [Description("Adiciona ou atualiza referências externas para a obra.")]
    public async Task<IResult> UpdateAsync(
        [AutocompleteProvider(AutocompleteTitles.Id)]
        [Description("Nome da obra")]
        string title,
        [Description("Nome da referência")]
        ExternalReference reference,
        [Description("Valor da referência")]
        string value)
    {
        var request = new Request(title, reference, value);

        var result = await handler.ExecuteAsync(request, CancellationToken);
        var embed = result.IsSuccess
            ? EmbedBuilder.CreateSuccessEmbed($"Referência atualizada!")
            : EmbedBuilder.CreateErrorEmbed(result);

        return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }
}
