using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class PublishBloggerStep(
    GoogleBloggerService googleBloggerService,
    TextReplacer textReplacer) : IPublishStep
{
    public StepType Type => StepType.Publish;
    public StepName Name => StepName.PublishBlogspot;
    public StepName? Dependency => null;

    public Task<Result> ValidateAsync(State state, CancellationToken _) =>
        Task.FromResult(Result.Ok());

    public async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var updatedState = state with { BloggerImageAsBase64 = await googleBloggerService.CreatePostCoverAsync(state.CoverFilePath, cancellationToken) };
        var template = googleBloggerService.GetPostTemplate();
        var htmlContent = textReplacer.Replace(template, updatedState);

        // todo: parametrizar valores de title abaixo no futuro
        var post = await googleBloggerService.PostAsync(
            title: $"[{updatedState.Title.Name}] Capítulo {updatedState.ChapterInfo.ChapterNumber}",
            htmlContent: htmlContent,
            label: updatedState.Title.Name,
            chapterNumber: updatedState.ChapterInfo.ChapterNumber,
            cancellationToken);

        if (post.IsFailed)
            return post.ToResult<State>();

        return Result.Ok(updatedState with { BloggerLink = post.Value.Url });
    }
}
