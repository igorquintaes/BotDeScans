using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.State.Models;

public class StepInfoTests : UnitTest
{
    public class StepStatusProp : StepInfoTests
    {
        [Fact]
        public void GivenManagementStepTypeShouldCreateStepInfoWithQueuedForExecutionStatus() =>
            new StepInfo(A.Fake<IManagementStep>()).Status.Should().Be(StepStatus.QueuedForExecution);

        [Fact]
        public void GivenPublishStepTypeShouldCreateStepInfoWithQueuedForValidationStatus() =>
            new StepInfo(A.Fake<IPublishStep>()).Status.Should().Be(StepStatus.QueuedForValidation);
    }

    public class UpdateStatus : StepInfoTests
    {
        [Fact]
        public void GivenManagementStepTypeShouldChangeStatusToSuccessWhenResultIsSuccess()
        {
            var stepInfo = new StepInfo(A.Fake<IManagementStep>());
            stepInfo.UpdateStatus(Result.Ok());

            stepInfo.Status.Should().Be(StepStatus.Success);
        }

        [Fact]
        public void GivenPublishStepTypeInFirstCallShouldChangeStatusToQueuedToExecutionWhenResultIsSuccess()
        {
            var stepInfo = new StepInfo(A.Fake<IPublishStep>());
            stepInfo.UpdateStatus(Result.Ok());

            stepInfo.Status.Should().Be(StepStatus.QueuedForExecution);
        }

        [Fact]
        public void GivenPublishStepTypeInSecondCallShouldChangeStatusToSuccessWhenResultIsSuccess()
        {
            var stepInfo = new StepInfo(A.Fake<IPublishStep>());
            stepInfo.UpdateStatus(Result.Ok());
            stepInfo.UpdateStatus(Result.Ok());

            stepInfo.Status.Should().Be(StepStatus.Success);
        }

        [Fact]
        public void GivenManagementStepTypeShouldChangeStatusToErrorWhenResultIsFailed()
        {
            var stepInfo = new StepInfo(A.Fake<IManagementStep>());
            stepInfo.UpdateStatus(Result.Fail("some message"));

            stepInfo.Status.Should().Be(StepStatus.Error);
        }

        [Fact]
        public void GivenPublishStepTypeInFirstCallShouldChangeStatusToErrorWhenResultIsFailed()
        {
            var stepInfo = new StepInfo(A.Fake<IPublishStep>());
            stepInfo.UpdateStatus(Result.Fail("some message"));

            stepInfo.Status.Should().Be(StepStatus.Error);
        }

        [Fact]
        public void GivenPublishStepTypeInSecondCallShouldChangeStatusToErrorWhenResultIsFailed()
        {
            var stepInfo = new StepInfo(A.Fake<IPublishStep>());
            stepInfo.UpdateStatus(Result.Ok());
            stepInfo.UpdateStatus(Result.Fail("some message"));

            stepInfo.Status.Should().Be(StepStatus.Error);
        }
    }
}
