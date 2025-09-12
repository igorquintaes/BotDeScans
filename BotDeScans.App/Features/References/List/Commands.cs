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
    [Description("Obtém uma lista com todas referências de uma obra.")]
    public async Task<IResult> ExecuteAsync(
        [AutocompleteProvider(AutocompleteTitles.ID)]
        [Description("Nome da obra")]
        int title)
    {
        var result = await handler.ExecuteAsync(title, CancellationToken);
        var embed = EmbedBuilder.CreateSuccessEmbed(string.Join(Environment.NewLine, result));

        return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }
}
