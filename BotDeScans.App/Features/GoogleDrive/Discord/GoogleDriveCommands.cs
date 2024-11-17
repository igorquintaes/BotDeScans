using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Discord;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Objects;
using Remora.Results;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Features.GoogleDrive.Discord;

[Group("googledrive")]
public class GoogleDriveCommands(
    GoogleDriveService googleDriveService,
    GoogleDriveSettingsService googleDriveSettingsService,
    ExtendedFeedbackService feedbackService,
    ChartService chartService) : CommandGroup
{
    [Command("verify-url")]
    [RoleAuthorize("Staff")]
    [Description("Verifica se a url contém ou não erros para publicação")]
    public async Task<IResult> VerifyUrl(string link)
    {
        var result = await googleDriveService.ValidateFilesFromLinkAsync(link);
        if (result.IsFailed)
            return await result.PostErrorOnDiscord(feedbackService, CancellationToken);

        var successEmbed = EmbedBuilder.CreateSuccessEmbed("Os arquivos do link estão de acordo com as regras de publicação!");
        return await feedbackService.SendContextualEmbedAsync(successEmbed, ct: CancellationToken);
    }

    [Command("delete-file")]
    [RoleAuthorize("Publisher")]
    [Description("Deleta arquivo no google drive, baseado no nome da pasta e nome do arquivo")]
    public async Task<IResult> DeleteFile(string fileName, string folderName)
    {
        var deleteResult = await googleDriveService.DeleteFileByNameAndParentNameAsync(fileName, folderName, CancellationToken);
        var embed = deleteResult.IsSuccess
            ? EmbedBuilder.CreateSuccessEmbed($"Arquivo deletado com sucesso!")
            : EmbedBuilder.CreateErrorEmbed(deleteResult);

        return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    [Command("grant-data-access")]
    [RoleAuthorize("Publisher")]
    [Description("Adiciona permissão de leitura para arquivos gerenciados pelo bot.")]
    public async Task<IResult> GrantDataAccess(string email)
    {
        var accessResult = await googleDriveService.GrantReaderAccessToBotFilesAsync(email, CancellationToken);
        var embed = accessResult.IsSuccess
            ? EmbedBuilder.CreateSuccessEmbed($"Acesso concedido!")
            : EmbedBuilder.CreateErrorEmbed(accessResult);

        return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    [Command("revoke-data-access")]
    [RoleAuthorize("Publisher")]
    [Description("Revoga permissão de leitura para arquivos gerenciados pelo bot.")]
    public async Task<IResult> RevokeDataAccess(string email)
    {
        var accessResult = await googleDriveService.RevokeReaderAccessToBotFilesAsync(email, CancellationToken);
        var embed = accessResult.IsSuccess
            ? EmbedBuilder.CreateSuccessEmbed($"Acesso revogado!")
            : EmbedBuilder.CreateErrorEmbed(accessResult);

        return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    [Command("data-usage")]
    [RoleAuthorize("Staff")]
    [Description("Obtém informação do uso de dados e espaço livre.")]
    public async Task<IResult> DataUsage()
    {
        var dataUsageResult = await googleDriveSettingsService.GetConsumptionDataAsync(CancellationToken);
        if (dataUsageResult.IsFailed)
            return await feedbackService
                .SendContextualEmbedAsync(EmbedBuilder
                .CreateErrorEmbed(dataUsageResult), ct: CancellationToken);

        var usageInfo = dataUsageResult.Value;
        await using var chartStream = chartService.CreatePieChart(
            new Dictionary<string, double>
            {
                { "Utilizado", usageInfo.UsedSpace },
                { "Livre", usageInfo.FreeSpace },

            });

        var fileName = $"{nameof(DataUsage)}.png";
        return await feedbackService.SendContextualEmbedAsync(
            embed: EmbedBuilder.CreateSuccessEmbed(
                title: "Uso de dados do Google Drive:",
                image: new EmbedImage($"attachment://{fileName}")),
            options: new FeedbackMessageOptionsBuilder()
                .WithAttachment(fileName, chartStream)
                .Build(),
            ct: CancellationToken);
    }

#if DEBUG
    [Group("debug")]
    [ExcludeFromCodeCoverage(Justification = "Live Discord testing and debug.")]
    public class GoogleDriveDebugCommands(
        GoogleDriveService googleDriveService,
        ExtendedFeedbackService feedbackService) : CommandGroup
    {
        private readonly GoogleDriveService googleDriveService = googleDriveService;
        private readonly ExtendedFeedbackService feedbackService = feedbackService;

        [Command("download-files")]
        [RoleAuthorize("Staff")]
        [Description("Baixa imagens do Google Drive")]
        public async Task<IResult> DownloadFiles(string url)
        {
            var accessResult = await googleDriveService.SaveFilesFromLinkAsync(url, Directory.GetCurrentDirectory());
            var embed = accessResult.IsSuccess
                ? EmbedBuilder.CreateSuccessEmbed($"Deu bom!")
                : EmbedBuilder.CreateErrorEmbed(accessResult);

            return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
        }

        [Command("upload-files")]
        [RoleAuthorize("Staff")]
        [Description("Sobe imagens no Google Drive ")]
        public async Task<IResult> UploadFiles()
        {
            var createFolderResult = await googleDriveService.GetOrCreateFolderAsync("algo", default, CancellationToken);
            if (createFolderResult.IsFailed)
                return await feedbackService.SendContextualEmbedAsync(
                    EmbedBuilder.CreateErrorEmbed(createFolderResult),
                    ct: CancellationToken);

            var createFileResult = await googleDriveService.CreateFileAsync(
                Path.Combine(Directory.GetCurrentDirectory(), "ALGO.zip"),
                createFolderResult.Value.Id,
                false,
                CancellationToken);

            var embed = createFileResult.IsSuccess
                ? EmbedBuilder.CreateSuccessEmbed($"Deu bom!")
                : EmbedBuilder.CreateErrorEmbed(createFileResult);

            return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
        }
    }

#endif
}
