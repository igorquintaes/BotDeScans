using BotDeScans.App.Attributes;
using BotDeScans.App.Builders;
using BotDeScans.App.Extensions;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Features.GoogleDrive.Models;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Features.GoogleDrive;

[Group("googledrive")]
public class Commands(
    GoogleDriveService googleDriveService,
    GoogleDriveSettingsService googleDriveSettingsService,
    IFeedbackService feedbackService,
    ChartService chartService,
    IValidator<GoogleDriveUrl> googleDriveUrlValidator) : CommandGroup
{
    [Command("verify-url")]
    [RoleAuthorize("Staff")]
    [Description("Verifica se a url contém ou não erros para publicação")]
    public async Task<IResult> VerifyUrl(string url)
    {
        var validationResult = await googleDriveUrlValidator.ValidateAsync(new GoogleDriveUrl(url), CancellationToken);
        var responseEmbed = validationResult.IsValid
            ? EmbedBuilder.CreateSuccessEmbed("Arquivos válidos para publicação!")
            : EmbedBuilder.CreateErrorEmbed(validationResult.ToResult());

        return await feedbackService.SendContextualEmbedAsync(responseEmbed, ct: CancellationToken);
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
        await using var chartStream = chartService.CreatePieChart(usageInfo);

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
    public class DebugCommands(
        GoogleDriveService googleDriveService,
        ExtendedFeedbackService feedbackService) : CommandGroup
    {
        private readonly GoogleDriveService googleDriveService = googleDriveService;
        private readonly ExtendedFeedbackService feedbackService = feedbackService;

        [Command("download-files")]
        [RoleAuthorize("Staff")]
        [Description("Tests Google Drive download.")]
        public async Task<IResult> DownloadFiles(string url)
        {
            var googleDriveUrl = new GoogleDriveUrl(url);
            var downloadResult = await googleDriveService.SaveFilesAsync(googleDriveUrl.Id, Directory.GetCurrentDirectory(), CancellationToken);
            if (downloadResult.IsFailed)
                return await downloadResult.PostErrorOnDiscord(feedbackService, CancellationToken);

            return await feedbackService.SendContextualEmbedAsync(
                EmbedBuilder.CreateSuccessEmbed($"Funcionando."),
                ct: CancellationToken);
        }

        [Command("upload-files")]
        [RoleAuthorize("Staff")]
        [Description("Tests Google Drive upload. Expects a 'debug.zip' file in app root folder to run this command.")]
        public async Task<IResult> UploadFiles()
        {
            const string DEBUG_NAME_FOLDER = "debug";
            const string DEBUG_NAME_FILE = "debug.zip";
            var createFolderResult = await googleDriveService.GetOrCreateFolderAsync(DEBUG_NAME_FOLDER, default, CancellationToken);
            if (createFolderResult.IsFailed)
                return await feedbackService.SendContextualEmbedAsync(EmbedBuilder.CreateErrorEmbed(createFolderResult), ct: CancellationToken);

            var parentId = createFolderResult.Value.Id;
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), DEBUG_NAME_FILE);
            var createFileResult = await googleDriveService.CreateFileAsync(filePath, parentId, false, CancellationToken);

            var embed = createFileResult.IsFailed
                 ? EmbedBuilder.CreateErrorEmbed(createFileResult)
                 : EmbedBuilder.CreateSuccessEmbed($"Working.");

            return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
        }
    }

#endif
}
