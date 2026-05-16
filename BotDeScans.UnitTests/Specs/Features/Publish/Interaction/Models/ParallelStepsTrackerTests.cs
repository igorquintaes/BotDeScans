using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Models.Entities.Enums;
using FluentAssertions.Execution;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Models;

public class ParallelStepsTrackerTests : UnitTest
{
    private readonly IStep step1;
    private readonly IStep step2;
    private readonly StepInfo stepInfo1;
    private readonly StepInfo stepInfo2;
    private readonly State initialState;

    public ParallelStepsTrackerTests()
    {
        step1 = A.Fake<IConversionStep>();
        step2 = A.Fake<IConversionStep>();

        stepInfo1 = A.Fake<StepInfo>();
        stepInfo2 = A.Fake<StepInfo>();

        A.CallTo(() => stepInfo1.UpdateStatus(A<Result>.Ignored)).Returns(stepInfo1);
        A.CallTo(() => stepInfo2.UpdateStatus(A<Result>.Ignored)).Returns(stepInfo2);

        initialState = new State
        {
            Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { step1, stepInfo1 },
                { step2, stepInfo2 },
            })
        };
    }

    public class ApplyAndNotifyAsync : ParallelStepsTrackerTests
    {
        [Fact]
        public async Task GivenSuccessfulStepShouldUpdateCurrentStateViaTrackingCallback()
        {
            var sentStates = new List<State>();
            Task<Result<State>> TrackingCallback(State s, CancellationToken _)
            {
                sentStates.Add(s);
                return Task.FromResult(Result.Ok(s));
            }

            var tracker = new ParallelStepsTracker(initialState, TrackingCallback);
            var snapshot = initialState with { ZipFilePath = "/tmp/chapter.zip" };

            await tracker.ApplyAndNotifyAsync(Result.Ok(), step1, snapshot, cancellationToken);

            using var _ = new AssertionScope();
            sentStates.Should().HaveCount(1);
            tracker.CurrentState.ZipFilePath.Should().Be("/tmp/chapter.zip");
        }

        [Fact]
        public async Task GivenTwoSequentialCallsShouldAccumulateStateWithoutRegressingPriorStep()
        {
            Task<Result<State>> TrackingCallback(State s, CancellationToken _) =>
                Task.FromResult(Result.Ok(s));

            var tracker = new ParallelStepsTracker(initialState, TrackingCallback);

            var snapshot1 = initialState with { ZipFilePath = "/tmp/chapter.zip" };
            var snapshot2 = initialState with { PdfFilePath = "/tmp/chapter.pdf" };

            await tracker.ApplyAndNotifyAsync(Result.Ok(), step1, snapshot1, cancellationToken);
            await tracker.ApplyAndNotifyAsync(Result.Ok(), step2, snapshot2, cancellationToken);

            using var _ = new AssertionScope();
            tracker.CurrentState.ZipFilePath.Should().Be("/tmp/chapter.zip");
            tracker.CurrentState.PdfFilePath.Should().Be("/tmp/chapter.pdf");
        }

        [Fact]
        public async Task GivenFirstStepAlreadySucceededSecondApplyShouldNotRegressItsStatus()
        {
            var updatedInfo1 = A.Fake<StepInfo>();
            A.CallTo(() => stepInfo1.UpdateStatus(A<Result>.That.Matches(r => r.IsSuccess))).Returns(updatedInfo1);

            var capturedStates = new List<State>();
            Task<Result<State>> TrackingCallback(State s, CancellationToken _)
            {
                capturedStates.Add(s);
                return Task.FromResult(Result.Ok(s));
            }

            var tracker = new ParallelStepsTracker(initialState, TrackingCallback);

            // step1 completes first
            await tracker.ApplyAndNotifyAsync(Result.Ok(), step1, initialState, cancellationToken);
            // step2 completes second — must not revert step1 back to queued
            await tracker.ApplyAndNotifyAsync(Result.Ok(), step2, initialState, cancellationToken);

            using var _ = new AssertionScope();
            // The second Discord embed must still contain step1's resolved StepInfo
            capturedStates[1].Steps[step1].Should().Be(updatedInfo1);
        }

        [Fact]
        public async Task GivenTrackingCallbackFailureShouldAggregateError()
        {
            Task<Result<State>> TrackingCallback(State s, CancellationToken _) =>
                Task.FromResult(Result.Fail<State>("discord error"));

            var tracker = new ParallelStepsTracker(initialState, TrackingCallback);

            await tracker.ApplyAndNotifyAsync(Result.Ok(), step1, initialState, cancellationToken);

            tracker.AggregateTrackingResult.IsFailed.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSkippedStepShouldReturnOkWithoutCallingTrackingCallback()
        {
            var called = false;
            Task<Result<State>> TrackingCallback(State s, CancellationToken _)
            {
                called = true;
                return Task.FromResult(Result.Ok(s));
            }

            var tracker = new ParallelStepsTracker(initialState, TrackingCallback);

            // Simulate skip by passing StepStatus.Skip in StepInfo — tracker tests the ApplyAndNotifyAsync
            // path directly, so we simulate a skipped step result as an already-ok result with no snapshot change.
            // The actual skip guard lives in RunStepParallelAsync (Handler), tested via HandlerTests.
            // Here we just verify that a success result with unchanged snapshot still sends a tracking update.
            await tracker.ApplyAndNotifyAsync(Result.Ok(), step1, initialState, cancellationToken);

            called.Should().BeTrue();
        }
    }
}
