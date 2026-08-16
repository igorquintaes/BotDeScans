using BotDeScans.App.Extensions;
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
        var stateWithBloggerImage = state with { BloggerImageAsBase64 = await googleBloggerService.CreatePostCoverAsync(state.CoverFilePath, cancellationToken) };
        var template = googleBloggerService.GetPostTemplate();
        var htmlContent = textReplacer.Replace(template, stateWithBloggerImage);

        // todo: parametrizar valores de title abaixo no futuro
        var post = await googleBloggerService.PostAsync(
            title: $"[{stateWithBloggerImage.Title.Name}] Capítulo {stateWithBloggerImage.ChapterInfo.ChapterNumber}",
            htmlContent: htmlContent,
            label: stateWithBloggerImage.Title.Name,
            chapterNumber: stateWithBloggerImage.ChapterInfo.ChapterNumber,
            cancellationToken);

        var updatedState = stateWithBloggerImage with { BloggerLink = post.ValueOrDefault?.Url };

        return post.Map(updatedState);
    }
}
