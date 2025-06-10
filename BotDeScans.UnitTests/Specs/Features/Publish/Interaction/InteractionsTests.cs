using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Models.DTOs;
using FluentResults;
using Remora.Discord.API.Abstractions.Objects;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class InteractionsTests : UnitTest
{
    private readonly Interactions interactions;

    public InteractionsTests()
    {
        fixture.Freeze<State>();

        fixture.FreezeFake<DiscordPublisher>();
        fixture.FreezeFake<StepsService>();
        fixture.FreezeFake<Handler>();

        fixture.FreezeFake<Remora.Results.IResult<IMessage>>();

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
        public async Task GivenSuccessfulExecutionShouldFillPublishStateData()
        {
            var enabledSteps = fixture.Create<EnabledSteps>();
            A.CallTo(() => fixture
                .FreezeFake<StepsService>()
                .GetEnabledSteps())
                .Returns(enabledSteps);

            var info = fixture.Create<Info>();
            await interactions.ExecuteAsync(
                info.GoogleDriveUrl.Url,
                info.ChapterName!,
                info.ChapterNumber,
                info.ChapterVolume!,
                info.Message!,
                info.TitleId.ToString());

            fixture.Freeze<State>().ChapterInfo.Should().BeEquivalentTo(info);
            fixture.Freeze<State>().Steps.Should().BeEquivalentTo(enabledSteps);
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
