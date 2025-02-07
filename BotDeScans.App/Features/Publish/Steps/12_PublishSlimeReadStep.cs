using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Features.Publish.Steps;

public class PublishSlimeReadStep(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.PublishBlogspot;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
    {
        if (state.Title.References.All(x => x.Key != Models.ExternalReference.SlimeRead))
            return Task.FromResult(Result.Fail("Não foi definido uma referência para a publicação da obra na SlimeRead."));

        var username = configuration.GetValue("SlimeRead:Username", string.Empty);
        var password = configuration.GetValue("SlimeRead:Password", string.Empty);

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
            return Task.FromResult(Result.Fail("As configurações da SlimeRead não estão preenchidas (parcialmente ou totalmente)."));

        return Task.FromResult(Result.Ok());
    }

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var slimeReadService = serviceProvider.GetRequiredService<SlimeReadService>();

        var loginResult = await slimeReadService.LoginAsync(cancellationToken);
        if (loginResult.IsFailed)
            return loginResult;

        var titleReference = state.Title.References.Single(x => x.Key == Models.ExternalReference.SlimeRead);
        //var uploadResult = await slimeReadService.UploadChapterAsync(
        //    titleReference.Value,
        //    state.ReleaseInfo.ChapterName,
        //    state.ReleaseInfo.ChapterNumber,
        //    state.ReleaseInfo.ChapterVolume,
        //    state.InternalData.OriginContentFolder);

        //if (uploadResult.IsFailed)
        //    return uploadResult.ToResult();

        state.ReleaseLinks.SlimeReadLink = "";
        return Result.Ok();
    }
}