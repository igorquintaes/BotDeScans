using BotDeScans.App.Builders;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.Titles.Update;

public class Interactions(
    Handler handler,
    IFeedbackService feedbackService) : InteractionGroup
{
    public const string MODAL_NAME = "Titles.Update";

    [Modal(MODAL_NAME)]
    [Description("Atualiza dados da obra")]
    public async Task<IResult> ExecuteAsync(
    string name,
    string? role,
    string state)
    {
        var result = await handler.ExecuteAsync(name, role, int.Parse(state), CancellationToken);
        var embed = result.IsSuccess
            ? EmbedBuilder.CreateSuccessEmbed("Obra atualizada com sucesso!")
            : EmbedBuilder.CreateErrorEmbed(result);

        return await feedbackService.SendContextualEmbedAsync(embed: embed, ct: CancellationToken);
    }
}
