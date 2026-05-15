using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class HandlerTests : UnitTest
{
    private readonly Handler handler;
    private readonly State state;

    public HandlerTests()
    {
        fixture.FreezeFake<DiscordPublisher>();
        handler = fixture.Create<Handler>();
        state = new State();
    }

    public class ExecuteAsync : HandlerTests
    {
        private readonly State testState;

        public ExecuteAsync()
        {
            var mgmt1 = A.Fake<IManagementStep>();
            var mgmt2 = A.Fake<IManagementStep>();
            var pub1 = A.Fake<IPublishStep>();
            var pub2 = A.Fake<IPublishStep>();

            A.CallTo(() => pub1.Name).Returns(StepName.UploadZipGoogleDrive);
            A.CallTo(() => pub2.Name).Returns(StepName.UploadMangadex);

            A.CallTo(() => mgmt1.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => mgmt2.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => pub1.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => pub2.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => pub1.ValidateAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok());
            A.CallTo(() => pub2.ValidateAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok());

            var steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { mgmt1, A.Fake<StepInfo>() },
                { mgmt2, A.Fake<StepInfo>() },
                { pub1, A.Fake<StepInfo>() },
                { pub2, A.Fake<StepInfo>() },
            });

            testState = state with { Steps = steps };
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await handler.ExecuteAsync(testState, cancellationToken);
            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedQuantityOfUpdateTrackingMessage()
        {
            await handler.ExecuteAsync(testState, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(A<EnabledSteps>._, cancellationToken))
                .MustHaveHappened(7, Times.Exactly);
        }

        [Fact]
        public async Task GivenErrorExecutionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error message";
            var pub1 = testState.Steps.PublishSteps.First().Step;

            A.CallTo(() => pub1.ValidateAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.ExecuteAsync(testState, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExceptionExecutionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "Fatal error occurred. More information inside log file.";
            var pub1 = testState.Steps.PublishSteps.First().Step;

            A.CallTo(() => pub1.ValidateAsync(A<State>.Ignored, cancellationToken))
                .Throws(new Exception("some message."));

            var result = await handler.ExecuteAsync(testState, cancellationToken);
            result.Should().BeFailure().And
                .HaveError(ERROR_MESSAGE).And
                .Match(result =>
                    result.Errors.Count == 1 &&
                    result.Errors[0].Reasons
                        .Select(reason => reason.Message)
                        .Contains("some message."));
        }

        [Fact]
        public async Task GivenErrorUpdateTrackingMessageShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error message";
            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(A<EnabledSteps>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.ExecuteAsync(testState, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenStepToBeSkippedShouldNotCallItsValidatorNeitherItsPublisher()
        {
            var (publishStep1, publishInfo1) = testState.Steps.PublishSteps.First();
            var (publishStep2, _) = testState.Steps.PublishSteps.Last();

            A.CallTo(() => publishInfo1.Status).Returns(StepStatus.Skip);

            await handler.ExecuteAsync(testState, cancellationToken);

            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishInfo1.UpdateStatus(A<Result>.Ignored)).MustNotHaveHappened();

            A.CallTo(() => publishStep2.ValidateAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenManagementStepErrorShouldStopBeforeValidationPhase()
        {
            var mgmt1 = testState.Steps.ManagementSteps.First().Step;
            var (publishStep1, _) = testState.Steps.PublishSteps.First();
            var (publishStep2, _) = testState.Steps.PublishSteps.Last();

            A.CallTo(() => mgmt1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail("management error"));

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure().And.HaveError("management error");

            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ValidateAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorExecutionWhenStepAllowsContinueOnErrorShouldContinueChainCall()
        {
            var publishStep1 = testState.Steps.PublishSteps.First().Step;
            var publishStep2 = testState.Steps.PublishSteps.Last().Step;

            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail("some error message"));

            A.CallTo(() => publishStep1.ContinueOnError).Returns(true);

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure();

            A.CallTo(() => publishStep2.ValidateAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
        }
    }
}
