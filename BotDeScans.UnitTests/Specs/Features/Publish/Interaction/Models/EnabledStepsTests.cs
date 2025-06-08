using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentAssertions.Execution;
using System.Drawing;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Models;

public class EnabledStepsTests : UnitTest
{
    public class ManagementStepsProperty : EnabledStepsTests
    {
        [Fact]
        public void ShouldReturnOnlyManagementSteps()
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
            enabledSteps.ManagementSteps.Should().HaveCount(2);
            enabledSteps.ManagementSteps.ElementAt(0).Step.Should().Be(steps[1]);
            enabledSteps.ManagementSteps.ElementAt(1).Step.Should().Be(steps[3]);
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
            var enabledSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) },
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) },
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) }
            });

            A.CallTo(() => enabledSteps.ElementAt(0).Key.Name)
                            .Returns(StepName.Setup);

            A.CallTo(() => enabledSteps.ElementAt(1).Key.Name)
                            .Returns(StepName.Download);

            A.CallTo(() => enabledSteps.ElementAt(2).Key.Name)
                            .Returns(StepName.Compress);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(0).Value, StepStatus.Success);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(1).Value, StepStatus.Success);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(2).Value, StepStatus.Success);

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
            var enabledSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) },
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) },
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) }
            });

            A.CallTo(() => enabledSteps.ElementAt(0).Key.Name)
                            .Returns(StepName.Setup);

            A.CallTo(() => enabledSteps.ElementAt(1).Key.Name)
                            .Returns(StepName.Download);

            A.CallTo(() => enabledSteps.ElementAt(2).Key.Name)
                            .Returns(StepName.Compress);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(0).Value, StepStatus.Success);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(1).Value, StepStatus.QueuedForExecution);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(2).Value, StepStatus.QueuedForValidation);

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
            var enabledSteps = new EnabledSteps(new Dictionary<IStep, StepInfo>
            {
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) },
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) },
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) },
                { A.Fake<IStep>(), new StepInfo(A.Fake<IStep>()) }
            });

            A.CallTo(() => enabledSteps.ElementAt(0).Key.Name)
                            .Returns(StepName.Setup);

            A.CallTo(() => enabledSteps.ElementAt(1).Key.Name)
                            .Returns(StepName.Download);

            A.CallTo(() => enabledSteps.ElementAt(2).Key.Name)
                            .Returns(StepName.Compress);

            A.CallTo(() => enabledSteps.ElementAt(3).Key.Name)
                            .Returns(StepName.ZipFiles);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(0).Value, StepStatus.Success);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(1).Value, StepStatus.QueuedForExecution);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(2).Value, StepStatus.Error);

            typeof(StepInfo).GetProperty(nameof(StepInfo.Status))!
                            .SetValue(enabledSteps.ElementAt(3).Value, StepStatus.QueuedForValidation);

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
}
