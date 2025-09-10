using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Services.Discord.Autocomplete;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;

namespace BotDeScans.App.Features.Titles.SkipSteps;

[Group("title")]
public class Commands()
    : CommandGroup
{
    [Group("skip-steps")]
    public partial class ChildCommands(
    IFeedbackService feedbackService,
    Add.Handler addHandler,
    Remove.Handler removeHandler,
    List.Handler listHandler) : CommandGroup
    {
        [Command("add")]
        [RoleAuthorize("Staff")]
        [Description("Adiciona novo procedimento de publicação para ser ignorado")]
        public async Task<IResult> ExecuteAddAsync(
            [AutocompleteProvider(AutocompleteTitles.ID)]
            [Description("Nome da obra")]
            int title,
            [Description("Procedimento a ser ignorado")]
            StepName step)
        {
            // todo: remover enums obrigatórios em novo AutoCompleteProvider
            await addHandler.ExecuteAsync(title, step, CancellationToken);
            var embed = EmbedBuilder.CreateSuccessEmbed($"O procedimento '{step.GetDescription()}' será ignorado na publicação da obra desejada.");

            return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
        }

        [Command("remove")]
        [RoleAuthorize("Staff")]
        [Description("Remove procedimento de publicação da lista de ignorados")]
        public async Task<IResult> ExecuteRemoveAsync(
            [AutocompleteProvider(AutocompleteTitles.ID)]
            [Description("Nome da obra")]
            int title,
            [Description("Procedimento a ser considerado")]
            StepName step)
        {
            // todo: remover enums obrigatórios em novo AutoCompleteProvider
            await removeHandler.ExecuteAsync(title, step, CancellationToken);
            var embed = EmbedBuilder.CreateSuccessEmbed($"O procedimento '{step.GetDescription()}' não será mais ignorado na publicação da obra desejada.");

            return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
        }

        [Command("list")]
        [RoleAuthorize("Publisher")]
        [Description("Obtém uma lista com todos procedimentos de publicação a serem ignorados")]
        public async Task<IResult> ExecuteAsync(
    [       AutocompleteProvider(AutocompleteTitles.ID)]
            [Description("Nome da obra")]
            int title)
        {
            var result = await listHandler.ExecuteAsync(title, CancellationToken);
            var embed = EmbedBuilder.CreateSuccessEmbed(string.Join(Environment.NewLine, result));

            return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
        }
    }
}