using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using Google.Apis.Blogger.v3.Data;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class PublishBloggerStepTests : UnitTest
{
    private readonly PublishBloggerStep step;

    public PublishBloggerStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<GoogleBloggerService>();
        fixture.FreezeFake<PublishReplacerService>();

        step = fixture.Create<PublishBloggerStep>();
    }

    public class Properties : PublishBloggerStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Publish);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.PublishBlogspot);

        [Fact]
        public void ShouldHaveExpectedDependency() =>
            step.Dependency.Should().Be(null);
    }

    public class ValidateAsync : PublishBloggerStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : PublishBloggerStepTests
    {
        public ExecuteAsync()
        {
            var state = fixture.Freeze<PublishState>();
            var template = fixture.Create<string>();
            var replacedTemplate = fixture.Create<string>();


            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .CreatePostCoverAsync(cancellationToken))
                .Returns(fixture.Create<string>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .GetPostTemplateAsync(cancellationToken))
                .Returns(template);

            A.CallTo(() => fixture
                .FreezeFake<PublishReplacerService>()
                .Replace(template))
                .Returns(replacedTemplate);

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .PostAsync(
                    $"[{state.Title.Name}] Capítulo {state.ReleaseInfo.ChapterNumber}",
                    replacedTemplate,
                    state.Title.Name,
                    state.ReleaseInfo.ChapterNumber,
                    cancellationToken))
                .Returns(new Post());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSaveBloggerCoverInState()
        {
            var cover = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .CreatePostCoverAsync(cancellationToken))
                .Returns(cover);

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().InternalData.BloggerImageAsBase64.Should().Be(cover);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSavePostUrlInState()
        {
            var url = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .PostAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    cancellationToken))
                .Returns(new Post() { Url = url });

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().ReleaseLinks.Blogger.Should().Be(url);
        }
    }
}
