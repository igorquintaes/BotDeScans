using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.CapivaraTradutor;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadCapivaraTradutorStep(
    CapivaraTradutorService capivaraTradutorService) : IPublishStep
{
    public StepName? Dependency => StepName.ZipFiles;

    public StepName Name => StepName.UploadCapivaraTradutor;
    public StepType Type => StepType.Upload;

    public bool ContinueOnError => true;

    // Todo: validate based on https://capivaratradutor.com/GROUP-SLUG/admin/api?tab=docs
    public Task<Result> ValidateAsync(State state, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Ok()); 

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var titleReference = state.Title.References.Single(x => x.Key == ExternalReference.CapivaraTradutor);
        var uploadResult = await capivaraTradutorService.UploadAsync(
            state.ChapterInfo,
            titleReference!.Value,
            state.OriginContentFolder,
            cancellationToken);

        var updatedState = state with { CapivaraTradutorLink = uploadResult.ValueOrDefault };

        return uploadResult.Map(updatedState);
    }
}
