using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class PublishMangaDexStep(
    MangaDexService mangaDexService,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadMangadex;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
    {
        if (state.Title.References.All(x => x.Key != Models.ExternalReference.MangaDex))
            return Task.FromResult(Result.Fail("Não foi definido uma referência para a publicação da obra na MangaDex."));

        return Task.FromResult(Result.Ok());
    }

    // API validations
    // (OK) 1 active upload session per account -> You need to either commit or abandon your current upload session before starting a new one 
    // (OK) Max 10 files per one PUT request
    // (OK) File format must be JPEG, PNG, or GIF
    // (NOK) Max 20MB per file
    // (NOK) Max 500 files per upload session
    // (NOK) Max 150MB per upload session
    // (NOK) File resolution must be below 10'000 pixels in both width and height 
    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {        
        var clearResult = await mangaDexService.ClearPendingUploadsAsync();
        if (clearResult.IsFailed)
            return clearResult;

        var mangaDexReference = state.Title.References.Single(x => x.Key == Models.ExternalReference.MangaDex);
        var uploadResult = await mangaDexService.UploadChapterAsync(
            mangaDexReference.Value,
            state.ReleaseInfo.ChapterName,
            state.ReleaseInfo.ChapterNumber,
            state.ReleaseInfo.ChapterVolume,
            state.InternalData.OriginContentFolder,
            cancellationToken);

        if (uploadResult.IsFailed)
            return uploadResult.ToResult();

        state.ReleaseLinks.MangaDexLink = $"https://mangadex.org/chapter/{uploadResult.Value}/1";
        return Result.Ok();
    }
}