using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Wrappers;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;
using Microsoft.Extensions.Configuration;
using static Google.Apis.Blogger.v3.PostsResource;

namespace BotDeScans.UnitTests.Specs.Services;

public class GoogleBloggerServiceTests : UnitTest
{
    private readonly GoogleBloggerService service;

    public GoogleBloggerServiceTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<ImageService>();
        fixture.FreezeFake<BloggerService>();
        fixture.FreezeFake<GoogleWrapper>();
        fixture.FreezeFake<IConfiguration>();

        service = fixture.Create<GoogleBloggerService>();
    }

    public class PostAsync : GoogleBloggerServiceTests
    {
        private readonly Post expectedPost;

        public PostAsync()
        {
            expectedPost = fixture.Create<Post>();

            var bloggerUrl = "http://www.escoladescans.com/";
            var bloggerId = fixture.Create<string>();

            fixture.FreezeFakeConfiguration("Blogger:Url", bloggerUrl);
            fixture.FreezeFakeConfiguration("Blogger:Id", fixture.Create<string>());

            A.CallTo(() => fixture
                .FreezeFake<BloggerService>().Posts)
                .Returns(fixture.FreezeFake<PostsResource>());

            A.CallTo(() => fixture
                .FreezeFake<PostsResource>()
                .Insert(A<Post>._, bloggerId))
                .Returns(fixture.FreezeFake<InsertRequest>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(A<InsertRequest>._, cancellationToken))
                .Returns(expectedPost);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResultWithPost()
        {
            var result = await service.PostAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(expectedPost);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldRequestPostWithExpectedData()
        {
            const string title = "T!tL3";
            const string htmlContent = "some-content";
            const string label = "some-label";
            const string chapterNumber = "NuMb3r";

            await service.PostAsync(title, htmlContent, label, chapterNumber, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<PostsResource>()
                .Insert(
                    A<Post>.That.Matches(post => 
                        post.Content == htmlContent &&
                        post.Title == title &&
                        post.Labels.Count == 1 &&
                        post.Labels.Single() == label &&
                        post.Url == "www.escoladescans.com/ttl3-numb3r"), 
                    A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }
    }
}
