using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.MangaDex;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class UploadMangaDexStep(
    MangaDexService mangaDexService,
    PublishState state) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadMangadex;
    public StepName? Dependency => StepName.ZipFiles;

    // API validations
    // (NOK) Max 20MB per file
    // (NOK) Max 500 files per upload session
    // (NOK) File resolution must be below 10'000 pixels in both width and height 
    public Task<Result> ValidateAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var mangaDexReference = state.Title.References.Single(x => x.Key == ExternalReference.MangaDex);
        var uploadResult = await mangaDexService.UploadAsync(
            state.ChapterInfo,
            mangaDexReference.Value,
            state.InternalData.OriginContentFolder,
            cancellationToken);

        if (uploadResult.IsFailed)
            return uploadResult.ToResult();

        state.ReleaseLinks.MangaDex = $"https://mangadex.org/chapter/{uploadResult.Value.Id}/1";
        return Result.Ok();
    }
}