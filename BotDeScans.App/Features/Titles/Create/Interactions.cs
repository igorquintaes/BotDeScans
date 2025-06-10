using BotDeScans.App.Builders;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.Titles.Create;

public class Interactions(
    Handler handler,
    IFeedbackService feedbackService) : InteractionGroup
{
    public const string MODAL_NAME = "Titles.Create";

    [Modal(MODAL_NAME)]
    [Description("Cadastra nova obra")]
    public async Task<IResult> ExecuteAsync(
        string name,
        string role)
    {
        var result = await handler.ExecuteAsync(name, role, CancellationToken);
        var embed = result.IsSuccess
            ? EmbedBuilder.CreateSuccessEmbed("Obra cadastrada com sucesso!")
            : EmbedBuilder.CreateErrorEmbed(result);

        return await feedbackService.SendContextualEmbedAsync(embed: embed, ct: CancellationToken);
    }
}
