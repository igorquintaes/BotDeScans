using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Discord;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Objects;
using Remora.Results;
using System.ComponentModel;
namespace BotDeScans.App.Features.Mega.Discord;

[Group("mega")]
public class MegaCommands(
    MegaSettingsService megaSettingsService,
    ExtendedFeedbackService feedbackService,
    ChartService chartService) : CommandGroup
{
    [Command("data-usage")]
    [RoleAuthorize("Staff")]
    [Description("Obtém informação do uso de dados e espaço livre.")]
    public async Task<IResult> DataUsage()
    {
        var root = await megaSettingsService.GetRootFolderAsync();
        var dataUsageResult = await megaSettingsService.GetConsumptionDataAsync(root.Id);
        if (dataUsageResult.IsFailed)
            return await feedbackService
                .SendContextualEmbedAsync(EmbedBuilder
                .CreateErrorEmbed(dataUsageResult), ct: CancellationToken);

        var usageInfo = dataUsageResult.Value;
        await using var chartStream = chartService.CreatePieChart(usageInfo);

        var fileName = $"{nameof(DataUsage)}.png";
        return await feedbackService.SendContextualEmbedAsync(
            embed: EmbedBuilder.CreateSuccessEmbed(
                title: "Uso de dados do Mega:",
                image: new EmbedImage($"attachment://{fileName}")),
            options: new FeedbackMessageOptionsBuilder()
                .WithAttachment(fileName, chartStream)
                .Build(),
            ct: CancellationToken);
    }
}
