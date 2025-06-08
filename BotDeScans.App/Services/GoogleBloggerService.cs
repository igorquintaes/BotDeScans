using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services;

public class GoogleBloggerService(
    State state,
    ImageService imageService,
    BloggerService bloggerService,
    IConfiguration configuration)
{
    public const string TEMPLATE_FILE_NAME = "blogger-template.html";

    public virtual async Task<Post> PostAsync(
        string title,
        string htmlContent,
        string label,
        string chapterNumber,
        CancellationToken cancellationToken)
    {
        var bloggerUrl = configuration.GetRequiredValue<string>("Blogger:Url");
        var bloggerId = configuration.GetRequiredValue<string>("Blogger:Id");

        var uri = new Uri(bloggerUrl, UriKind.Absolute);
        var post = new Post()
        {
            Content = htmlContent,
            Title = title,
            Labels = [label],
            Url = $"{uri.Host}{title.Slugfy()}-{chapterNumber.Slugfy()}"
        };

        var insertRequest = bloggerService.Posts.Insert(post, bloggerId);
        return await insertRequest.ExecuteAsync(cancellationToken);
    }

    public virtual async Task<string> GetPostTemplateAsync(CancellationToken cancellationToken)
    {
        var templatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config",
            TEMPLATE_FILE_NAME);

        using var streamReader = new StreamReader(templatePath);
        return await streamReader.ReadToEndAsync(cancellationToken);
    }

    public virtual async Task<string> CreatePostCoverAsync(CancellationToken cancellationToken)
    {
        var width = configuration.GetRequiredValue<int>("Blogger:Cover:Width");
        var height = configuration.GetRequiredValue<int>("Blogger:Cover:Height");

        var isGrayScale = imageService.IsGrayscale(state.InternalData.CoverFilePath);
        var cover = await imageService.CreateBase64StringAsync(state.InternalData.CoverFilePath, width, height, isGrayScale, cancellationToken);

        return $"data:image/png;base64,{cover}";
    }
}
