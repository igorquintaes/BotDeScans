using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Services.ExternalClients;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services;

public partial class GoogleBloggerService(
    PublishState state,
    ImageService imageService,
    BloggerClient bloggerClient,
    IConfiguration configuration)
{
    public const string BLOGGER_RELEASE_TEMPLATE_FILE_NAME = "blogger-template.html";
    private readonly BloggerService bloggerService = bloggerClient.Client;

    public async Task<Post> PostAsync(
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
            BLOGGER_RELEASE_TEMPLATE_FILE_NAME);

        using var streamReader = new StreamReader(templatePath);
        return await streamReader.ReadToEndAsync(cancellationToken);
    }

    public virtual async Task<string> GetPostCoverAsync(CancellationToken cancellationToken)
    {
        var width = configuration.GetValue<int?>("Blogger:Cover:Width") ?? 200;
        var height = configuration.GetValue<int?>("Blogger:Cover:Height") ?? 300;

        var isGrayScale = imageService.IsGrayscale(state.InternalData.CoverFilePath);
        var cover = await imageService.CreateBase64StringAsync(state.InternalData.CoverFilePath, width, height, isGrayScale, cancellationToken);

        return $"data:image/png;base64,{cover}";
    }
}
