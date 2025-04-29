using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class StepsServiceTests : UnitTest
{
    private readonly StepsService service;

    public StepsServiceTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<PublishMessageService>();
        service = fixture.Create<StepsService>();
    }

    public class ExecuteAsync : StepsServiceTests
    {
        public ExecuteAsync()
        {
            fixture.Freeze<PublishState>().Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { A.Fake<IManagementStep>(), A.Fake<StepInfo>() },
                { A.Fake<IManagementStep>(), A.Fake<StepInfo>() },
                { A.Fake<IPublishStep>(), A.Fake<StepInfo>() },
                { A.Fake<IPublishStep>(), A.Fake<StepInfo>() },
            });
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.ExecuteAsync(cancellationToken);
            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedChainMethods()
        {
            await service.ExecuteAsync(cancellationToken);

            var firstManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.First().Step;
            var secondManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.Last().Step;
            var firstPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.First().Step;
            var secondPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.Last().Step;

            A.CallTo(() => firstManagementStep.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly()
                .Then(A.CallTo(() => secondManagementStep.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstPublishStep.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => secondPublishStep.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstPublishStep.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => secondPublishStep.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedQuantityOfUpdateTrackingMessage()
        {
            await service.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .MustHaveHappened(7, Times.Exactly);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedQuantityOfUpdateStepStatus()
        {
            await service.ExecuteAsync(cancellationToken);

            var firstManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.First();
            var secondManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.Last();
            var firstPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.First();
            var secondPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.Last();

            A.CallTo(() => firstManagementStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => secondManagementStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => firstPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedTwiceExactly();
            A.CallTo(() => secondPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedTwiceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExpectedChain()
        {
            await service.ExecuteAsync(cancellationToken);

            var firstManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.First();
            var secondManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.Last();
            var firstPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.First();
            var secondPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.Last();

            A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened()
                .Then(A.CallTo(() => firstManagementStep.Step.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstManagementStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => secondManagementStep.Step.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => secondManagementStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => firstPublishStep.Step.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => secondPublishStep.Step.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => secondPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => firstPublishStep.Step.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => secondPublishStep.Step.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => secondPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened());
        }

        [Fact]
        public async Task GivenErrorExecutionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error message";
            A.CallTo(() => fixture
                .Freeze<PublishState>()
                .Steps.PublishSteps.First().Step
                .ValidateAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.ExecuteAsync(cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorExecutionShouldBreakChainCall()
        {
            var firstManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.First();
            var secondManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.Last();
            var firstPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.First();
            var secondPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.Last();

            A.CallTo(() => firstPublishStep.Step
                .ValidateAsync(cancellationToken))
                .Returns(Result.Fail("some error message"));

            await service.ExecuteAsync(cancellationToken);

            // Call chain
            A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened()
                .Then(A.CallTo(() => firstManagementStep.Step.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstManagementStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => secondManagementStep.Step.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => secondManagementStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => firstPublishStep.Step.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened());

            // Not executed due error
            A.CallTo(() => firstPublishStep.Step.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => secondPublishStep.Step.ValidateAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => secondPublishStep.Step.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => secondPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustNotHaveHappened();

            // Update tracking error times
            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .MustHaveHappened(4, Times.Exactly);

            // Publish step was interrupted in validation, so its UpdateStatus must be called once exactly
            A.CallTo(() => firstPublishStep.Info
                .UpdateStatus(A<Result>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorUpdateTrackingMessageShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error message";
            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.ExecuteAsync(cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorUpdateTrackingMessageShouldBreakChainCall()
        {
            var firstManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.First();
            var secondManagementStep = fixture.Freeze<PublishState>().Steps.ManagementSteps.Last();
            var firstPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.First();
            var secondPublishStep = fixture.Freeze<PublishState>().Steps.PublishSteps.Last();

            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .ReturnsNextFromSequence(Result.Ok(), Result.Ok(), Result.Ok(), Result.Fail("some error message"));

            await service.ExecuteAsync(cancellationToken);

            // Call chain
            A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened()
                .Then(A.CallTo(() => firstManagementStep.Step.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstManagementStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => secondManagementStep.Step.ExecuteAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => secondManagementStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened())
                .Then(A.CallTo(() => firstPublishStep.Step.ValidateAsync(cancellationToken)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => firstPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustHaveHappened())
                .Then(A.CallTo(() => fixture.FreezeFake<PublishMessageService>().UpdateTrackingMessageAsync(cancellationToken)).MustHaveHappened());

            // Not executed due error
            A.CallTo(() => firstPublishStep.Step.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => secondPublishStep.Step.ValidateAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => secondPublishStep.Step.ExecuteAsync(cancellationToken)).MustNotHaveHappened();
            A.CallTo(() => secondPublishStep.Info.UpdateStatus(A<Result>.Ignored)).MustNotHaveHappened();

            // Update tracking error times
            A.CallTo(() => fixture
                .FreezeFake<PublishMessageService>()
                .UpdateTrackingMessageAsync(cancellationToken))
                .MustHaveHappened(4, Times.Exactly);

            // Publish step was interrupted in validation, so its UpdateStatus must be called once exactly
            A.CallTo(() => firstPublishStep.Info
                .UpdateStatus(A<Result>.Ignored))
                .MustHaveHappenedOnceExactly();
        }
    }

    private abstract class FakePublisStep : IManagementStep
    {
        public virtual StepType Type => throw new NotImplementedException();
        public virtual StepName Name => throw new NotImplementedException();
        public virtual bool IsMandatory => throw new NotImplementedException();

        public virtual Task<Result> ExecuteAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private abstract class FakePublishStep : IPublishStep
    {
        public virtual StepType Type => throw new NotImplementedException();
        public virtual StepName Name => throw new NotImplementedException();
        public virtual StepName? Dependency => throw new NotImplementedException();

        public virtual Task<Result> ExecuteAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public virtual Task<Result> ValidateAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
