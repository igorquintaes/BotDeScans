using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class PublishBloggerStep(
    IConfiguration configuration,
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.PublishBlogspot;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
    {
        var bloggerUrl = configuration.GetValue<string?>("Blogger:Url");
        var bloggerId = configuration.GetValue<string?>("Blogger:Id");

        if (string.IsNullOrWhiteSpace(bloggerUrl) ||
            string.IsNullOrWhiteSpace(bloggerId))
            return Task.FromResult(Result.Fail("As configurações do Blogger não estão preenchias (parcialmente ou totalmente)."));

        if (Uri.TryCreate(bloggerUrl, UriKind.Absolute, out var _) is false)
            return Task.FromResult(Result.Fail("Não foi possível identificar o link do Blogger como válido."));

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var templateFileName = GoogleBloggerService.BLOGGER_RELEASE_TEMPLATE_FILE_NAME;
        var templateFilePath = Path.Combine(baseDirectory, "config", templateFileName);
        if (!File.Exists(templateFilePath))
            return Task.FromResult(Result.Fail($"Não foi possível encontrar o arquivo de template : {templateFileName}"));

        return Task.FromResult(Result.Ok());
    }

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var googleBloggerService = serviceProvider.GetRequiredService<GoogleBloggerService>();
        var publishReplacerService = serviceProvider.GetRequiredService<PublishReplacerService>();

        state.InternalData.BloggerImageAsBase64 = await googleBloggerService.GetPostCoverAsync(cancellationToken);
        var template = await googleBloggerService.GetPostTemplateAsync(cancellationToken);
        var htmlContent =  publishReplacerService.Replace(template);

        // todo: parametrizar valores abaixo no futuro
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
