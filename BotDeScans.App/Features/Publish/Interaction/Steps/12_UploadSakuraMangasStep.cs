using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadSakuraMangasStep(
    SakuraMangasService sakuraMangasService,
    IPublishContext context) : IPublishStep
{
    public StepName? Dependency => StepName.ZipFiles;

    public StepName Name => StepName.UploadSakuraMangas;

    public StepType Type => StepType.Upload;

    public bool ContinueOnError => true;

    public Task<Result> ValidateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Ok()); // Todo: validação de arquivo com max de 100mb (pós zip)

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var uploadResult = await sakuraMangasService.UploadAsync(
            context.ChapterInfo.ChapterNumber,
            context.ChapterInfo.ChapterName,
            context.Title.References.Single(x => x.Key == ExternalReference.SakuraMangas).Value,
            context.ZipFilePath!,
            cancellationToken);

        if (uploadResult.IsFailed)
            return uploadResult.ToResult();

        context.SetSakuraMangasLink(uploadResult.Value);
        return Result.Ok();
    }
}
