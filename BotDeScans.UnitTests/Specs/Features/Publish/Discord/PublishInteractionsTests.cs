using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Features.Publish.Steps;
using Remora.Discord.API.Abstractions.Objects;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Discord;

public class PublishInteractionsTests : UnitTest
{
    private readonly PublishInteractions interactions; 

    public PublishInteractionsTests()
    {
        fixture.Freeze<PublishState>();

        fixture.FreezeFake<PublishMessageService>();
        fixture.FreezeFake<PublishService>();
        fixture.FreezeFake<StepsService>();

        fixture.FreezeFake<Remora.Results.IResult<IMessage>>();

        A.CallTo(() => fixture
            .FreezeFake<Remora.Results.IResult<IMessage>>().IsSuccess)
            .Returns(true);

        A.CallTo(() => fixture
            .FreezeFake<PublishMessageService>()
            .SuccessReleaseMessageAsync(cancellationToken))
            .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

        interactions = fixture.CreateCommand<PublishInteractions>(cancellationToken); 
    }

    public class PublishAsync : PublishInteractionsTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await interactions.PublishAsync(default!, default!, default!, default!, default!, "1");

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldFillPublishStateData()
        {
            var enabledSteps = fixture.Create<EnabledSteps>();
            A.CallTo(() => fixture
                .FreezeFake<PublishService>()
                .GetEnabledSteps())
                .Returns(enabledSteps);

            var info = fixture.Create<Info>();
            await interactions.PublishAsync(
                info.GoogleDriveUrl.Url,
                info.ChapterName!,
                info.ChapterNumber,
                info.ChapterVolume!,
                info.Message!,
                info.TitleId.ToString());

            fixture.Freeze<PublishState>().ReleaseInfo.Should().BeEquivalentTo(info);
            fixture.Freeze<PublishState>().Steps.Should().BeEquivalentTo(enabledSteps);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallSuccessReleaseMessage()
        {
            await interactions.PublishAsync(default!, default!, default!, default!, default!, "1");

            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .SuccessReleaseMessageAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToExecuteStepsShouldCallErrorMessageAsync()
        {
            var stepsResult = Result.Fail("some error.");

            A.CallTo(() => fixture
                .FreezeFake<StepsService>()
                .ExecuteAsync(cancellationToken))
                .Returns(stepsResult);

            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .ErrorReleaseMessageAsync(stepsResult, cancellationToken))
                .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

            var result = await interactions.PublishAsync(default!, default!, default!, default!, default!, "1");
            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
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
                .FreezeFake<PublishMessageService>()
                .SuccessReleaseMessageAsync(cancellationToken))
                .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

            var result = await interactions.PublishAsync(default!, default!, default!, default!, default!, "1");

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GivenErrorToPublishErrorMessageShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<StepsService>()
                .ExecuteAsync(cancellationToken))
                .Returns(Result.Fail("some error."));

            A.CallTo(() => fixture
                .FreezeFake<Remora.Results.IResult<IMessage>>().IsSuccess)
                .Returns(false);

            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .ErrorReleaseMessageAsync(A<Result>.Ignored, cancellationToken))
                .Returns(fixture.FreezeFake<Remora.Results.IResult<IMessage>>());

            var result = await interactions.PublishAsync(default!, default!, default!, default!, default!, "1");

            result.IsSuccess.Should().BeFalse();
        }
    }
}
