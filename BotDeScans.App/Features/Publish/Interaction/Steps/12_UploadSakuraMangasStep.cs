using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadSakuraMangasStep(
    SakuraMangasService sakuraMangasService) : IPublishStep
{
    public StepName? Dependency => StepName.ZipFiles;

    public StepName Name => StepName.UploadSakuraMangas;

    public StepType Type => StepType.Upload;

    public bool ContinueOnError => true;

    public Task<Result> ValidateAsync(State state, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Ok()); // Todo: validação de arquivo com max de 100mb (pós zip)

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var uploadResult = await sakuraMangasService.UploadAsync(
            state.ChapterInfo.ChapterNumber,
            state.ChapterInfo.ChapterName,
            state.Title.References.Single(x => x.Key == ExternalReference.SakuraMangas).Value,
            state.ZipFilePath!,
            cancellationToken);

        if (uploadResult.IsFailed)
            return uploadResult.ToResult<State>();

        return Result.Ok(state with { SakuraMangasLink = uploadResult.Value });
    }
}
