using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class PublishBloggerStep(
    GoogleBloggerService googleBloggerService,
    TextReplacer textReplacer,
    IPublishContext context) : IPublishStep
{
    public StepType Type => StepType.Publish;
    public StepName Name => StepName.PublishBlogspot;
    public StepName? Dependency => null;

    public Task<Result> ValidateAsync(CancellationToken _) =>
        Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        context.SetBloggerImageAsBase64(await googleBloggerService.CreatePostCoverAsync(cancellationToken));
        var template = googleBloggerService.GetPostTemplate();
        var htmlContent = textReplacer.Replace(template);

        // todo: parametrizar valores de title abaixo no futuro
        var post = await googleBloggerService.PostAsync(
            title: $"[{context.Title.Name}] Capítulo {context.ChapterInfo.ChapterNumber}",
            htmlContent: htmlContent,
            label: context.Title.Name,
            chapterNumber: context.ChapterInfo.ChapterNumber,
            cancellationToken);

        if (post.IsFailed)
            return post.ToResult();

        context.SetBloggerLink(post.Value.Url);
        return Result.Ok();
    }
}
