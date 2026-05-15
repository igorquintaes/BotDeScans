using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Models.DTOs;
using FluentResults;
using Remora.Discord.API.Abstractions.Objects;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class InteractionsTests : UnitTest
{
    private readonly Interactions interactions;

    public InteractionsTests()
    {
        fixture.FreezeFake<DiscordPublisher>();
        fixture.FreezeFake<SetupService>();
        fixture.FreezeFake<Handler>();

        fixture.FreezeFake<Remora.Results.IResult<IMessage>>();

        A.CallTo(() => fixture
            .FreezeFake<SetupService>()
            .SetupAsync(A<Info>._, cancellationToken))
            .Returns(Result.Ok());

        A.CallTo(() => fixture
            .FreezeFake<Remora.Results.IResult<IMessage>>().IsSuccess)
            .Returns(true);

        A.CallTo(() => fixture
            .FreezeFake<DiscordPublisher>()
            .SuccessReleaseMessageAsync(cancellationToken))
            .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

        interactions = fixture.CreateCommand<Interactions>(cancellationToken);
    }

    public class ExecuteAsync : InteractionsTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await interactions.ExecuteAsync(default!, default!, default!, default!, default!, "1");

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallSetupWithChapterInfo()
        {
            var info = fixture.Create<Info>();
            await interactions.ExecuteAsync(
                info.GoogleDriveUrl.Url,
                info.ChapterName!,
                info.ChapterNumber,
                info.ChapterVolume!,
                info.Message!,
                info.TitleId.ToString());

            A.CallTo(() => fixture
                .FreezeFake<SetupService>()
                .SetupAsync(info, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallSuccessReleaseMessage()
        {
            await interactions.ExecuteAsync(default!, default!, default!, default!, default!, "1");

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .SuccessReleaseMessageAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSetupErrorShouldCallErrorReleaseMessageAsync()
        {
            var setupResult = Result.Fail("setup error.");

            A.CallTo(() => fixture
                .FreezeFake<SetupService>()
                .SetupAsync(A<Info>._, cancellationToken))
                .Returns(setupResult);

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .ErrorReleaseMessageAsync(setupResult, cancellationToken))
                .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

            await interactions.ExecuteAsync(default!, default!, default!, default!, default!, "1");

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .ErrorReleaseMessageAsync(setupResult, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSetupErrorShouldNotCallHandler()
        {
            A.CallTo(() => fixture
                .FreezeFake<SetupService>()
                .SetupAsync(A<Info>._, cancellationToken))
                .Returns(Result.Fail("setup error."));

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .ErrorReleaseMessageAsync(A<Result>.Ignored, cancellationToken))
                .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

            await interactions.ExecuteAsync(default!, default!, default!, default!, default!, "1");

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorToExecuteStepsShouldCallErrorMessageAsync()
        {
            var stepsResult = Result.Fail("some error.");

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(cancellationToken))
                .Returns(stepsResult);

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .ErrorReleaseMessageAsync(stepsResult, cancellationToken))
                .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

            var result = await interactions.ExecuteAsync(default!, default!, default!, default!, default!, "1");
            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .ErrorReleaseMessageAsync(stepsResult, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToPublishSuccessMessageShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<Remora.Results.IResult<IMessage>>().IsSuccess)
                .Returns(false);

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .SuccessReleaseMessageAsync(cancellationToken))
                .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

            var result = await interactions.ExecuteAsync(default!, default!, default!, default!, default!, "1");

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GivenErrorToPublishErrorMessageShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(cancellationToken))
                .Returns(Result.Fail("some error."));

            A.CallTo(() => fixture
                .FreezeFake<Remora.Results.IResult<IMessage>>().IsSuccess)
                .Returns(false);

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .ErrorReleaseMessageAsync(A<Result>.Ignored, cancellationToken))
                .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

            var result = await interactions.ExecuteAsync(default!, default!, default!, default!, default!, "1");

            result.IsSuccess.Should().BeFalse();
        }
    }
}
