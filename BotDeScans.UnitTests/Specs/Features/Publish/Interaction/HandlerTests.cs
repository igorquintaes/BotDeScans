using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentAssertions.Execution;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class HandlerTests : UnitTest
{
    private readonly Handler handler;
    private readonly State state;

    public HandlerTests()
    {
        fixture.FreezeFake<DiscordPublisher>();

        A.CallTo(() => fixture
            .FreezeFake<DiscordPublisher>()
            .UpdateTrackingMessageAsync(A<State>._, A<CancellationToken>._))
            .ReturnsLazily((State s, CancellationToken _) => Result.Ok(s));

        A.CallTo(() => fixture
            .FreezeFake<DiscordPublisher>()
            .SynchronizedUpdateTrackingMessageAsync(A<State>._, A<CancellationToken>._))
            .ReturnsLazily((State s, CancellationToken _) => Result.Ok(s));

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

            // publishStep1 depends on ZipFiles (group 1), publishStep2 has no dependency (group 2).
            // This ensures the two publish steps are in different DAG groups so cross-group
            // break-chain behavior can be verified deterministically.
            A.CallTo(() => publishStep1.Dependency).Returns(StepName.ZipFiles);
            A.CallTo(() => publishStep2.Dependency).Returns(null);

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

            A.CallTo(() => managementStepInfo1.UpdateStatus(A<Result>.Ignored)).Returns(managementStepInfo1);
            A.CallTo(() => managementStepInfo2.UpdateStatus(A<Result>.Ignored)).Returns(managementStepInfo2);
            A.CallTo(() => publishStepInfo1.UpdateStatus(A<Result>.Ignored)).Returns(publishStepInfo1);
            A.CallTo(() => publishStepInfo2.UpdateStatus(A<Result>.Ignored)).Returns(publishStepInfo2);

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
        public async Task GivenSuccessfulExecutionShouldCallExpectedQuantityOfSynchronizedUpdateTrackingMessage()
        {
            await handler.ExecuteAsync(testState, cancellationToken);

            // 2 management + 2 validation + 2 publish = 6 synchronized calls
            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .SynchronizedUpdateTrackingMessageAsync(A<State>._, cancellationToken))
                .MustHaveHappened(6, Times.Exactly);

            // 1 initial tracking call (non-synchronized)
            A.CallTo(() => fixture
                .FreezeFake<DiscordPublisher>()
                .UpdateTrackingMessageAsync(A<State>._, cancellationToken))
                .MustHaveHappenedOnceExactly();
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
                .SynchronizedUpdateTrackingMessageAsync(A<State>._, cancellationToken))
                .Returns(Result.Fail<State>(ERROR_MESSAGE));

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

            // Management and validation phases are always sequential.
            // Publish steps are in separate DAG groups (step1: ZipFiles dep, step2: no dep),
            // so group 1 completes before group 2 starts, preserving end-to-end order.
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
            // publishStep1 is in group 1 (Dependency=ZipFiles), publishStep2 is in group 2 (Dependency=null).
            // A fatal failure in group 1 stops group 2 from running.
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
            // publishStep1 is in group 1 (Dependency=ZipFiles), publishStep2 is in group 2 (Dependency=null).
            // An unhandled exception in group 1 stops group 2 from running.
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
                .UpdateTrackingMessageAsync(A<State>._, cancellationToken))
                .Returns(Result.Fail<State>("tracking error"));

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
            var updatedState = state with { Pings = "updated" };

            A.CallTo(() => managementStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Ok(updatedState));

            await handler.ExecuteAsync(testState, cancellationToken);

            A.CallTo(() => managementStep2.ExecuteAsync(
                A<State>.That.Matches(s => s.Pings == "updated"),
                cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenPublishExecutionErrorWithContinueOnErrorShouldContinueChainCall()
        {
            // publishStep1 is in group 1, publishStep2 is in group 2.
            // When group 1 fails but ContinueOnError=true, the DAG continues to group 2.
            A.CallTo(() => publishStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail<State>("publish error"));

            A.CallTo(() => publishStep1.ContinueOnError).Returns(true);

            var result = await handler.ExecuteAsync(testState, cancellationToken);

            result.Should().BeFailure();

            A.CallTo(() => publishStep2.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenParallelStepsInSameGroupShouldMergeTheirOutputStates()
        {
            // Two publish steps that share the same dependency group run in parallel.
            // Their individual State outputs (each setting a different link) must be merged.
            var parallelStep1 = A.Fake<IPublishStep>();
            var parallelStep2 = A.Fake<IPublishStep>();

            A.CallTo(() => parallelStep1.Name).Returns(StepName.UploadZipMega);
            A.CallTo(() => parallelStep2.Name).Returns(StepName.UploadZipBox);
            A.CallTo(() => parallelStep1.Dependency).Returns(StepName.ZipFiles);
            A.CallTo(() => parallelStep2.Dependency).Returns(StepName.ZipFiles);

            A.CallTo(() => parallelStep1.ValidateAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok());
            A.CallTo(() => parallelStep2.ValidateAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok());

            A.CallTo(() => parallelStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .ReturnsLazily((State s, CancellationToken _) => Result.Ok(s with { MegaZipLink = "https://mega.nz/zip" }));
            A.CallTo(() => parallelStep2.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .ReturnsLazily((State s, CancellationToken _) => Result.Ok(s with { BoxZipLink = "https://box.com/zip" }));

            var parallelStepInfo1 = A.Fake<StepInfo>();
            var parallelStepInfo2 = A.Fake<StepInfo>();
            A.CallTo(() => parallelStepInfo1.UpdateStatus(A<Result>.Ignored)).Returns(parallelStepInfo1);
            A.CallTo(() => parallelStepInfo2.UpdateStatus(A<Result>.Ignored)).Returns(parallelStepInfo2);

            var parallelSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { managementStep1, managementStepInfo1 },
                { parallelStep1, parallelStepInfo1 },
                { parallelStep2, parallelStepInfo2 },
            });
            var parallelState = state with { Steps = parallelSteps };

            var result = await handler.ExecuteAsync(parallelState, cancellationToken);

            result.Should().BeSuccess();
            result.Value.MegaZipLink.Should().Be("https://mega.nz/zip");
            result.Value.BoxZipLink.Should().Be("https://box.com/zip");
        }
    }

    public class ConversionStepsTests : HandlerTests
    {
        private readonly IConversionStep conversionStep1;
        private readonly IConversionStep conversionStep2;
        private readonly StepInfo conversionStepInfo1;
        private readonly StepInfo conversionStepInfo2;
        private readonly IManagementStep managementStep;
        private readonly StepInfo managementStepInfo;
        private readonly State conversionTestState;

        public ConversionStepsTests()
        {
            managementStep = A.Fake<IManagementStep>();
            conversionStep1 = A.Fake<IConversionStep>();
            conversionStep2 = A.Fake<IConversionStep>();

            A.CallTo(() => managementStep.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => conversionStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .ReturnsLazily((State s, CancellationToken _) => Result.Ok(s with { ZipFilePath = "/tmp/chapter.zip" }));
            A.CallTo(() => conversionStep2.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .ReturnsLazily((State s, CancellationToken _) => Result.Ok(s with { PdfFilePath = "/tmp/chapter.pdf" }));

            managementStepInfo = A.Fake<StepInfo>();
            conversionStepInfo1 = A.Fake<StepInfo>();
            conversionStepInfo2 = A.Fake<StepInfo>();

            A.CallTo(() => managementStepInfo.UpdateStatus(A<Result>.Ignored)).Returns(managementStepInfo);
            A.CallTo(() => conversionStepInfo1.UpdateStatus(A<Result>.Ignored)).Returns(conversionStepInfo1);
            A.CallTo(() => conversionStepInfo2.UpdateStatus(A<Result>.Ignored)).Returns(conversionStepInfo2);

            var steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { managementStep, managementStepInfo },
                { conversionStep1, conversionStepInfo1 },
                { conversionStep2, conversionStepInfo2 },
            });

            conversionTestState = state with { Steps = steps };
        }

        [Fact]
        public async Task GivenConversionStepsShouldRunThemAfterManagementPhase()
        {
            var callOrder = new List<string>();

            A.CallTo(() => managementStep.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("management.Execute"))
                .Returns(Result.Ok(state));
            A.CallTo(() => conversionStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("conversion1.Execute"))
                .ReturnsLazily((State s, CancellationToken _) => Result.Ok(s with { ZipFilePath = "/tmp/chapter.zip" }));
            A.CallTo(() => conversionStep2.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Invokes(() => callOrder.Add("conversion2.Execute"))
                .ReturnsLazily((State s, CancellationToken _) => Result.Ok(s with { PdfFilePath = "/tmp/chapter.pdf" }));

            await handler.ExecuteAsync(conversionTestState, cancellationToken);

            callOrder[0].Should().Be("management.Execute");
            callOrder.Should().Contain("conversion1.Execute");
            callOrder.Should().Contain("conversion2.Execute");
        }

        [Fact]
        public async Task GivenConversionStepsShouldMergeTheirOutputFilePathsIntoState()
        {
            var result = await handler.ExecuteAsync(conversionTestState, cancellationToken);

            result.Should().BeSuccess();
            result.Value.ZipFilePath.Should().Be("/tmp/chapter.zip");
            result.Value.PdfFilePath.Should().Be("/tmp/chapter.pdf");
        }

        [Fact]
        public async Task GivenConversionStepErrorShouldStopBeforePublishPhase()
        {
            var publishStep = A.Fake<IPublishStep>();
            A.CallTo(() => publishStep.Dependency).Returns(null);
            A.CallTo(() => publishStep.ValidateAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok());
            A.CallTo(() => publishStep.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));

            var publishStepInfo = A.Fake<StepInfo>();
            A.CallTo(() => publishStepInfo.UpdateStatus(A<Result>.Ignored)).Returns(publishStepInfo);

            A.CallTo(() => conversionStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail<State>("conversion error"));

            var steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { managementStep, managementStepInfo },
                { conversionStep1, conversionStepInfo1 },
                { publishStep, publishStepInfo },
            });
            var stateWithPublish = state with { Steps = steps };

            var result = await handler.ExecuteAsync(stateWithPublish, cancellationToken);

            result.Should().BeFailure().And.HaveError("conversion error");
            A.CallTo(() => publishStep.ValidateAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => publishStep.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenConversionStepWithContinueOnErrorShouldStillRunSiblingConversionStep()
        {
            A.CallTo(() => conversionStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail<State>("conversion error"));
            A.CallTo(() => conversionStep1.ContinueOnError).Returns(true);

            await handler.ExecuteAsync(conversionTestState, cancellationToken);

            // conversionStep2 still ran in parallel despite conversionStep1 failing
            A.CallTo(() => conversionStep2.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenConversionStepWithContinueOnErrorShouldContinueToPublishPhase()
        {
            var publishStep = A.Fake<IPublishStep>();
            var publishStepInfo = A.Fake<StepInfo>();
            A.CallTo(() => publishStep.Dependency).Returns(null);
            A.CallTo(() => publishStep.ValidateAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok());
            A.CallTo(() => publishStep.ExecuteAsync(A<State>.Ignored, cancellationToken)).Returns(Result.Ok(state));
            A.CallTo(() => publishStepInfo.UpdateStatus(A<Result>.Ignored)).Returns(publishStepInfo);

            A.CallTo(() => conversionStep1.ExecuteAsync(A<State>.Ignored, cancellationToken))
                .Returns(Result.Fail<State>("conversion error"));
            A.CallTo(() => conversionStep1.ContinueOnError).Returns(true);

            var steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { managementStep, managementStepInfo },
                { conversionStep1, conversionStepInfo1 },
                { conversionStep2, conversionStepInfo2 },
                { publishStep, publishStepInfo },
            });
            var stateWithPublish = state with { Steps = steps };

            var result = await handler.ExecuteAsync(stateWithPublish, cancellationToken);

            result.Should().BeFailure().And.HaveError("conversion error");
            A.CallTo(() => publishStep.ValidateAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
            A.CallTo(() => publishStep.ExecuteAsync(A<State>.Ignored, cancellationToken)).MustHaveHappenedOnceExactly();
        }
    }

    public class MergeStatesTests : HandlerTests
    {
        [Fact]
        public void GivenParallelResultsShouldMergeNonNullLinkProperties()
        {
            var baseState = new State { MegaZipLink = "https://mega.nz/zip" };
            var updatedState = new State
            {
                BoxZipLink = "https://box.com/zip",
                Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>()),
            };

            var merged = Handler.MergeStates(baseState, updatedState);

            merged.MegaZipLink.Should().Be("https://mega.nz/zip");
            merged.BoxZipLink.Should().Be("https://box.com/zip");
        }

        [Fact]
        public void GivenParallelConversionResultsShouldMergeFilePaths()
        {
            var baseState = new State { ZipFilePath = "/tmp/chapter.zip" };
            var updatedState = new State
            {
                PdfFilePath = "/tmp/chapter.pdf",
                Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>()),
            };

            var merged = Handler.MergeStates(baseState, updatedState);

            merged.ZipFilePath.Should().Be("/tmp/chapter.zip");
            merged.PdfFilePath.Should().Be("/tmp/chapter.pdf");
        }

        [Fact]
        public void GivenUpdatedLinkShouldOverrideBaseLink()
        {
            var baseState = new State { MegaZipLink = "https://old.link" };
            var updatedState = new State
            {
                MegaZipLink = "https://new.link",
                Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>()),
            };

            var merged = Handler.MergeStates(baseState, updatedState);

            merged.MegaZipLink.Should().Be("https://new.link");
        }

        [Fact]
        public void GivenNullUpdatedLinkShouldPreserveBaseLink()
        {
            var baseState = new State { DriveZipLink = "https://drive.google.com/zip" };
            var updatedState = new State
            {
                DriveZipLink = null,
                Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>()),
            };

            var merged = Handler.MergeStates(baseState, updatedState);

            merged.DriveZipLink.Should().Be("https://drive.google.com/zip");
        }

        [Fact]
        public void GivenParallelSnapshotsShouldPreserveStepInfoFromBothSnapshots()
        {
            var stepA = A.Fake<IConversionStep>();
            var stepB = A.Fake<IConversionStep>();

            var baseSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { stepA, new StepInfo(stepA) { Status = StepStatus.Success } },
                { stepB, new StepInfo(stepB) { Status = StepStatus.QueuedForExecution } },
            });

            var updatedSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { stepA, new StepInfo(stepA) { Status = StepStatus.QueuedForExecution } },
                { stepB, new StepInfo(stepB) { Status = StepStatus.Success } },
            });

            var baseState = new State { Steps = baseSteps };
            var updatedState = new State { Steps = updatedSteps };

            var merged = Handler.MergeStates(baseState, updatedState);

            using var _ = new AssertionScope();
            merged.Steps[stepA].Status.Should().Be(StepStatus.Success);
            merged.Steps[stepB].Status.Should().Be(StepStatus.Success);
        }

        [Fact]
        public void GivenUpdatedStepInfoWithDifferentStatusShouldOverrideBaseStepInfo()
        {
            var step = A.Fake<IConversionStep>();

            var baseSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { step, new StepInfo(step) { Status = StepStatus.QueuedForExecution } },
            });

            var updatedSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { step, new StepInfo(step) { Status = StepStatus.Success } },
            });

            var baseState = new State { Steps = baseSteps };
            var updatedState = new State { Steps = updatedSteps };

            var merged = Handler.MergeStates(baseState, updatedState);

            merged.Steps[step].Status.Should().Be(StepStatus.Success);
        }
    }
}
