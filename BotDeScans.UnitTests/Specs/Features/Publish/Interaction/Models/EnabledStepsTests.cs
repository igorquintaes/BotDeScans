using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentAssertions.Execution;
using System.Drawing;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Models;

public class EnabledStepsTests : UnitTest
{
    public class ManagementStepsProperty : EnabledStepsTests
    {
        [Fact]
        public void ShouldReturnOnlyManagementStepsExcludingConversionSteps()
        {
            var steps = new List<IStep>
            {
                A.Fake<IStep>(),
                A.Fake<IManagementStep>(),
                A.Fake<IConversionStep>(),
                A.Fake<IPublishStep>(),
                A.Fake<IManagementStep>(),
                A.Fake<IPublishStep>(),
            };

            var enabledSteps = new EnabledSteps(steps.ToDictionary(x => x, x => new StepInfo(x)));

            using var _ = new AssertionScope();
            enabledSteps.ManagementSteps.Should().HaveCount(2);
            enabledSteps.ManagementSteps.ElementAt(0).Step.Should().Be(steps[1]);
            enabledSteps.ManagementSteps.ElementAt(1).Step.Should().Be(steps[4]);
        }
    }

    public class ConversionStepsProperty : EnabledStepsTests
    {
        [Fact]
        public void ShouldReturnOnlyConversionSteps()
        {
            var steps = new List<IStep>
            {
                A.Fake<IStep>(),
                A.Fake<IManagementStep>(),
                A.Fake<IConversionStep>(),
                A.Fake<IPublishStep>(),
                A.Fake<IConversionStep>(),
            };

            var enabledSteps = new EnabledSteps(steps.ToDictionary(x => x, x => new StepInfo(x)));

            using var _ = new AssertionScope();
            enabledSteps.ConversionSteps.Should().HaveCount(2);
            enabledSteps.ConversionSteps.ElementAt(0).Step.Should().Be(steps[2]);
            enabledSteps.ConversionSteps.ElementAt(1).Step.Should().Be(steps[4]);
        }
    }

    public class PublishStepsProperty : EnabledStepsTests
    {
        [Fact]
        public void ShouldReturnOnlyPublishSteps()
        {
            var steps = new List<IStep>
            {
                A.Fake<IStep>(),
                A.Fake<IManagementStep>(),
                A.Fake<IPublishStep>(),
                A.Fake<IManagementStep>(),
                A.Fake<IPublishStep>(),
            };

            var enabledSteps = new EnabledSteps(steps.ToDictionary(x => x, x => new StepInfo(x)));

            using var _ = new AssertionScope();
            enabledSteps.PublishSteps.Should().HaveCount(2);
            enabledSteps.PublishSteps.ElementAt(0).Step.Should().Be(steps[2]);
            enabledSteps.PublishSteps.ElementAt(1).Step.Should().Be(steps[4]);
        }
    }

    public class OtherProperties : EnabledStepsTests
    {
        [Fact]
        public void GivenAllStepsWithSuccessStatusCollectionShouldHaveGreenColorStatus()
        {
            var step1 = A.Fake<IStep>();
            var step2 = A.Fake<IStep>();
            var step3 = A.Fake<IStep>();

            A.CallTo(() => step1.Name).Returns(StepName.Setup);
            A.CallTo(() => step2.Name).Returns(StepName.Download);
            A.CallTo(() => step3.Name).Returns(StepName.Compress);

            var enabledSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { step1, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.Success } },
                { step2, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.Success } },
                { step3, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.Success } }
            });

            using var _ = new AssertionScope();
            enabledSteps.ColorStatus.Should().Be(Color.Green);
            enabledSteps.MessageStatus.Should().Be("Executado com sucesso!");
            enabledSteps.Details.Should().Be(
                $":white_check_mark: - {StepName.Setup.GetDescription()}{Environment.NewLine}" +
                $":white_check_mark: - {StepName.Download.GetDescription()}{Environment.NewLine}" +
                $":white_check_mark: - {StepName.Compress.GetDescription()}");
        }

        [Fact]
        public void GivenStepsWithSuccessAndQueuedStatusCollectionShouldHaveLightBlueColorStatus()
        {
            var step1 = A.Fake<IStep>();
            var step2 = A.Fake<IStep>();
            var step3 = A.Fake<IStep>();

            A.CallTo(() => step1.Name).Returns(StepName.Setup);
            A.CallTo(() => step2.Name).Returns(StepName.Download);
            A.CallTo(() => step3.Name).Returns(StepName.Compress);

            var enabledSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { step1, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.Success } },
                { step2, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.QueuedForExecution } },
                { step3, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.QueuedForValidation } }
            });

            using var _ = new AssertionScope();
            enabledSteps.ColorStatus.Should().Be(Color.LightBlue);
            enabledSteps.MessageStatus.Should().Be("Processando...");
            enabledSteps.Details.Should().Be(
                $":white_check_mark: - {StepName.Setup.GetDescription()}{Environment.NewLine}" +
                $":clock10: - {StepName.Download.GetDescription()}{Environment.NewLine}" +
                $":clock9: - {StepName.Compress.GetDescription()}");
        }

        [Fact]
        public void GivenStepsWithEvenASingleErrorStatusCollectionShouldHaveRedColorStatus()
        {
            var step1 = A.Fake<IStep>();
            var step2 = A.Fake<IStep>();
            var step3 = A.Fake<IStep>();
            var step4 = A.Fake<IStep>();

            A.CallTo(() => step1.Name).Returns(StepName.Setup);
            A.CallTo(() => step2.Name).Returns(StepName.Download);
            A.CallTo(() => step3.Name).Returns(StepName.Compress);
            A.CallTo(() => step4.Name).Returns(StepName.ZipFiles);

            var enabledSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { step1, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.Success } },
                { step2, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.QueuedForExecution } },
                { step3, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.Error } },
                { step4, new StepInfo(A.Fake<IStep>()) { Status = StepStatus.QueuedForValidation } }
            });

            using var _ = new AssertionScope();
            enabledSteps.ColorStatus.Should().Be(Color.Red);
            enabledSteps.MessageStatus.Should().Be("Ocorreu um erro na execução!");
            enabledSteps.Details.Should().Be(
                $":white_check_mark: - {StepName.Setup.GetDescription()}{Environment.NewLine}" +
                $":clock10: - {StepName.Download.GetDescription()}{Environment.NewLine}" +
                $":warning: - {StepName.Compress.GetDescription()}{Environment.NewLine}" +
                $":clock9: - {StepName.ZipFiles.GetDescription()}");
        }
    }

    public class MergeWithMethod : EnabledStepsTests
    {
        [Fact]
        public void GivenTwoSnapshotsShouldPreserveUpdatedStatusFromOther()
        {
            var stepA = A.Fake<IConversionStep>();
            var stepB = A.Fake<IConversionStep>();

            var baseSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { stepA, new StepInfo(stepA) { Status = StepStatus.Success } },
                { stepB, new StepInfo(stepB) { Status = StepStatus.QueuedForExecution } },
            });

            var otherSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { stepA, new StepInfo(stepA) { Status = StepStatus.QueuedForExecution } },
                { stepB, new StepInfo(stepB) { Status = StepStatus.Success } },
            });

            var merged = baseSteps.MergeWith(otherSteps);

            using var _ = new AssertionScope();
            merged[stepA].Status.Should().Be(StepStatus.Success);
            merged[stepB].Status.Should().Be(StepStatus.Success);
        }

        [Fact]
        public void GivenSameStatusInBothSnapshotsShouldKeepBaseStepInfo()
        {
            var step = A.Fake<IConversionStep>();

            var baseSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { step, new StepInfo(step) { Status = StepStatus.Success } },
            });

            var otherSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { step, new StepInfo(step) { Status = StepStatus.Success } },
            });

            var merged = baseSteps.MergeWith(otherSteps);

            merged[step].Status.Should().Be(StepStatus.Success);
        }
    }
}
