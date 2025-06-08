using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class PublishBloggerStep(
    GoogleBloggerService googleBloggerService,
    TextReplacer textReplacer,
    State state) : IPublishStep
{
    public StepType Type => StepType.Publish;
    public StepName Name => StepName.PublishBlogspot;
    public StepName? Dependency => null;

    public Task<Result> ValidateAsync(CancellationToken _) =>
        Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        state.InternalData.BloggerImageAsBase64 = await googleBloggerService.CreatePostCoverAsync(cancellationToken);
        var template = await googleBloggerService.GetPostTemplateAsync(cancellationToken);
        var htmlContent = textReplacer.Replace(template);

        // todo: parametrizar valores de title abaixo no futuro
        var post = await googleBloggerService.PostAsync(
            title: $"[{state.Title.Name}] Capítulo {state.ChapterInfo.ChapterNumber}",
            htmlContent: htmlContent,
            label: state.Title.Name,
            chapterNumber: state.ChapterInfo.ChapterNumber,
            cancellationToken);

        state.ReleaseLinks.Blogger = post.Url;
        return Result.Ok();
    }
}
