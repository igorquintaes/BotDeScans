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
        private readonly IManagementStep managementStep1;
        private readonly IManagementStep managementStep2;
        private readonly IPublishStep publishStep1;
        private readonly IPublishStep publishStep2;
        private readonly StepInfo managementStepInfo1;
        private readonly StepInfo managementStepInfo2;
        private readonly StepInfo publishStepInfo1;
        private readonly StepInfo publishStepInfo2;

        public ExecuteAsync()
        {
            managementStep1 = A.Fake<IManagementStep>();
            managementStep2 = A.Fake<IManagementStep>();
            publishStep1 = A.Fake<IPublishStep>();
            publishStep2 = A.Fake<IPublishStep>();

            A.CallTo(() => publishStep1.Name).Returns(StepName.UploadZipGoogleDrive);
            A.CallTo(() => publishStep2.Name).Returns(StepName.UploadMangadex);

            A.CallTo(() => managementStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => managementStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok());
            A.CallTo(() => publishStep2.ValidateAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok());

            managementStepInfo1 = A.Fake<StepInfo>();
            managementStepInfo2 = A.Fake<StepInfo>();
            publishStepInfo1 = A.Fake<StepInfo>();
            publishStepInfo2 = A.Fake<StepInfo>();

            var steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { managementStep1, managementStepInfo1 },
                { managementStep2, managementStepInfo2 },
                { publishStep1, publishStepInfo1 },
                { publishStep2, publishStepInfo2 },
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

            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await handler.ExecuteAsync(testState, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExceptionExecutionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "Fatal error occurred. More information inside log file.";

            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken))
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
            A.CallTo(() => publishStepInfo1.Status).Returns(StepStatus.Skip);

            await handler.ExecuteAsync(testState, cancellationToken);

            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStepInfo1.UpdateStatus(A<Result>.Ignored)).MustNotHaveHappened();

            A.CallTo(() => publishStep2.ValidateAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenManagementStepErrorShouldStopBeforeValidationPhase()
        {
            A.CallTo(() => managementStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
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
            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail("some error message"));

            A.CallTo(() => publishStep1.ContinueOnError).Returns(true);

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure();

            A.CallTo(() => publishStep2.ValidateAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallChainInOrder_Management_Validation_Publish()
        {
            var callOrder = new List<string>();

            A.CallTo(() => managementStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("managementStep1.Execute"))
                .Returns(Result.Ok(state));
            A.CallTo(() => managementStep2.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("managementStep2.Execute"))
                .Returns(Result.Ok(state));
            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("publishStep1.Validate"))
                .Returns(Result.Ok());
            A.CallTo(() => publishStep2.ValidateAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("publishStep2.Validate"))
                .Returns(Result.Ok());
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("publishStep1.Execute"))
                .Returns(Result.Ok(state));
            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("publishStep2.Execute"))
                .Returns(Result.Ok(state));

            await handler.ExecuteAsync(testState, cancellationToken);

            callOrder.Should().Equal(
                "managementStep1.Execute",
                "managementStep2.Execute",
                "publishStep1.Validate",
                "publishStep2.Validate",
                "publishStep1.Execute",
                "publishStep2.Execute");
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallUpdateStatusForEveryStepPhase()
        {
            await handler.ExecuteAsync(testState, cancellationToken);

            A.CallTo(() => managementStepInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => managementStepInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishStepInfo1.UpdateStatus(A<Result>.Ignored)).MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => publishStepInfo2.UpdateStatus(A<Result>.Ignored)).MustHaveHappened(2, Times.Exactly);
        }

        [Fact]
        public async Task GivenValidationErrorShouldNotExecutePublishSteps()
        {
            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail("validation error"));

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure().And.HaveError("validation error");

            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenPublishExecutionErrorShouldBreakChainCall()
        {
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail<State>("publish execution error"));

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure().And.HaveError("publish execution error");

            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExceptionInManagementStepShouldBreakChainCall()
        {
            A.CallTo(() => managementStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Throws(new Exception("management exception"));

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure();

            A.CallTo(() => managementStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExceptionInPublishExecutionShouldBreakChainCall()
        {
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Throws(new Exception("publish exception"));

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure();

            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorUpdateTrackingMessageOnInitialCallShouldNotExecuteAnyStep()
        {
            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(A<EnabledSteps>._, cancellationToken))
                .Returns(Result.Fail("tracking error"));

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure().And.HaveError("tracking error");

            A.CallTo(() => managementStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => managementStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep1.ValidateAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenManagementStepStatePropagationShouldPassUpdatedStateToNextStep()
        {
            var updatedState = state with { InternalData = new() { Pings = "updated" } };

            A.CallTo(() => managementStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Ok(updatedState));

            await handler.ExecuteAsync(testState, cancellationToken);

            A.CallTo(() => managementStep2.ExecuteAsync(
                A<State>.That.Matches(s => s.InternalData.Pings == "updated"),
                cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenPublishExecutionErrorWithContinueOnErrorShouldContinueChainCall()
        {
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail<State>("publish error"));

            A.CallTo(() => publishStep1.ContinueOnError).Returns(true);

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure();

            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
        }
    }
}
