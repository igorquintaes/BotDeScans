using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;
using Google.Apis.Blogger.v3.Data;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class PublishBloggerStepTests : UnitTest
{
    private readonly PublishBloggerStep step;

    public PublishBloggerStepTests()
    {
        fixture.FreezeFake<IPublishContext>();
        fixture.FreezeFake<GoogleBloggerService>();
        fixture.FreezeFake<TextReplacer>();

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
            var title = fixture.Create<Title>();
            var chapterInfo = fixture.Create<Info>();
            var template = fixture.Create<string>();
            var replacedTemplate = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>().Title)
                .Returns(title);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>().ChapterInfo)
                .Returns(chapterInfo);

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .CreatePostCoverAsync(cancellationToken))
                .Returns(fixture.Create<string>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .GetPostTemplate())
                .Returns(template);

            A.CallTo(() => fixture
                .FreezeFake<TextReplacer>()
                .Replace(template))
                .Returns(replacedTemplate);

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .PostAsync(
                    $"[{title.Name}] Capítulo {chapterInfo.ChapterNumber}",
                    replacedTemplate,
                    title.Name,
                    chapterInfo.ChapterNumber,
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
        public async Task GivenSuccessfulExecutionShouldSaveBloggerCoverInContext()
        {
            var cover = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .CreatePostCoverAsync(cancellationToken))
                .Returns(cover);

            await step.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .SetBloggerImageAsBase64(cover))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSavePostUrlInContext()
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

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .SetBloggerLink(url))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToPostInBloggerShouldReturnErrorResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .PostAsync(A<string>._, A<string>._, A<string>._, A<string>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
