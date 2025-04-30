using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class PublishBloggerStep(
    GoogleBloggerService googleBloggerService,
    PublishReplacerService publishReplacerService,
    PublishState state) : IPublishStep
{
    public StepType Type => StepType.Publish;
    public StepName Name => StepName.PublishBlogspot;

    public Task<Result> ValidateAsync(CancellationToken _) =>
        Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        state.InternalData.BloggerImageAsBase64 = await googleBloggerService.CreatePostCoverAsync(cancellationToken);
        var template = await googleBloggerService.GetPostTemplateAsync(cancellationToken);
        var htmlContent = publishReplacerService.Replace(template);

        // todo: parametrizar valores de title abaixo no futuro
        var post = await googleBloggerService.PostAsync(
            title: $"[{state.Title.Name}] Capítulo {state.ReleaseInfo.ChapterNumber}",
            htmlContent: htmlContent,
            label: state.Title.Name,
            chapterNumber: state.ReleaseInfo.ChapterNumber,
            cancellationToken);

        state.ReleaseLinks.BloggerLink = post.Url;
        return Result.Ok();
    }
}
