using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class PublishBloggerStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.PublishBlogspot;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var googleBloggerService = serviceProvider.GetRequiredService<GoogleBloggerService>();

        var htmlContentResult = await googleBloggerService.GenerateHtmlAsync(state, cancellationToken);
        if (htmlContentResult.IsFailed)
            return htmlContentResult.ToResult();

        // todo: parametrizar valores abaixo no futuro
        var postResult = await googleBloggerService.PostAsync(
            title: $"[{state.Info.DisplayTitle}] Capítulo {state.Info.ChapterNumber}",
            htmlContent: htmlContentResult.Value,
            label: state.Info.DisplayTitle,
            chapterNumber: state.Info.ChapterNumber);

        if (postResult.IsFailed)
            return postResult.ToResult();

        state.Links.BloggerLink = postResult.Value.Url;
        return Result.Ok();
    }
}
