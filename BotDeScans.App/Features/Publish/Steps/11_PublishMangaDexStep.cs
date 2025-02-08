using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class PublishMangaDexStep(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadMangadex;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
    {
        if (state.Title.References.All(x => x.Key != Models.ExternalReference.MangaDex))
            return Task.FromResult(Result.Fail("Não foi definido uma referência para a publicação da obra na MangaDex."));

        var username = configuration.GetValue("Mangadex:Username", string.Empty);
        var password = configuration.GetValue("Mangadex:Password", string.Empty);
        var clientId = configuration.GetValue("Mangadex:ClientId", string.Empty);
        var clientSecret = configuration.GetValue("Mangadex:ClientSecret", string.Empty);
        var groupId = configuration.GetValue("Mangadex:GroupId", string.Empty);

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(groupId))
            return Task.FromResult(Result.Fail("As configurações da MangaDex não estão preenchidas (parcialmente ou totalmente)."));

        return Task.FromResult(Result.Ok());
    }

    // API validations
    // (OK) 1 active upload session per account -> You need to either commit or abandon your current upload session before starting a new one 
    // (OK) Max 10 files per one PUT request
    // Max 20MB per file
    // Max 500 files per upload session
    // Max 150MB per upload session
    // (OK) File format must be JPEG, PNG, or GIF
    // File resolution must be below 10'000 pixels in both width and height 
    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var mangaDexService = serviceProvider.GetRequiredService<MangaDexService>();

        var loginResult = await mangaDexService.LoginAsync();
        if (loginResult.IsFailed)
            return loginResult;

        var clearResult = await mangaDexService.ClearPendingUploadsAsync();
        if (clearResult.IsFailed)
            return clearResult;

        var mangaDexReference = state.Title.References.Single(x => x.Key == Models.ExternalReference.MangaDex);
        var uploadResult = await mangaDexService.UploadChapterAsync(
            mangaDexReference.Value,
            state.ReleaseInfo.ChapterName,
            state.ReleaseInfo.ChapterNumber,
            state.ReleaseInfo.ChapterVolume,
            state.InternalData.OriginContentFolder);

        if (uploadResult.IsFailed)
            return uploadResult.ToResult();

        state.ReleaseLinks.MangaDexLink = $"https://mangadex.org/chapter/{uploadResult.Value}/1";
        return Result.Ok();
    }
}