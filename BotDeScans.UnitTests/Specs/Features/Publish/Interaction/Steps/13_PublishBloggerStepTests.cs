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
    private readonly State state;

    public PublishBloggerStepTests()
    {
        fixture.FreezeFake<GoogleBloggerService>();
        fixture.FreezeFake<TextReplacer>();

        var title = fixture.Build<Title>()
            .With(x => x.Name, "Title X")
            .With(x => x.DiscordRoleId, 1UL)
            .Create();

        var chapterInfo = new Info(
            googleDriveUrl: "https://drive.google.com/drive/folders/1q2w3e4r5t6y7u8i9o",
            chapterName: "cap",
            chapterNumber: "1",
            chapterVolume: "1",
            message: "msg",
            titleId: 1);

        state = new State
        {
            Title = title,
            ChapterInfo = chapterInfo
        };

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
            var result = await step.ValidateAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : PublishBloggerStepTests
    {
        public ExecuteAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .CreatePostCoverAsync(cancellationToken))
                .Returns(fixture.Create<string>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .GetPostTemplate())
                .Returns(fixture.Create<string>());

            A.CallTo(() => fixture
                .FreezeFake<TextReplacer>()
                .Replace(A<string>.Ignored, A<State>.Ignored))
                .Returns(fixture.Create<string>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .PostAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    cancellationToken))
                .Returns(new Post { Url = fixture.Create<string>() });
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

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

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.BloggerImageAsBase64.Should().Be(cover);
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

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.BloggerLink.Should().Be(url);
        }

        [Fact]
        public async Task GivenErrorToPostInBloggerShouldReturnErrorResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleBloggerService>()
                .PostAsync(A<string>._, A<string>._, A<string>._, A<string>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
