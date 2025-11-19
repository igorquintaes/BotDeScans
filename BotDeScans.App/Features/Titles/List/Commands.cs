using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Infra.Repositories;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.Titles.List;

[Group("title")]
public class Commands(
    IFeedbackService feedbackService,
    TitleRepository titleRepository)
    : CommandGroup
{
    [Command("list")]
    [RoleAuthorize("Staff")]
    [Description("Obtém uma lista com todos as obras cadastradas")]
    public async Task<IResult> List()
    {
        // todo: pagination based on titles length (characters quantity) - discord api
        var titles = await titleRepository.GetTitlesAsync(CancellationToken);
        if (titles.Count == 0)
            return await feedbackService.SendContextualWarningAsync(
                "Não há obras cadastradas.",
                ct: CancellationToken);

        var titlesListAsText = titles
            .OrderBy(x => x.Name)
            .Select((x, index) => new { Number = index + 1, x.Name })
            .Select(x => string.Format("{0}. {1}", x.Number, x.Name));

        return await feedbackService.SendContextualEmbedAsync(
            embed: EmbedBuilder.CreateSuccessEmbed(
                title: "Lista de obras",
                description: string.Join(Environment.NewLine, titlesListAsText)),
            ct: CancellationToken);
    }
}