using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentAssertions.Execution;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class StateTests : UnitTest
{
    private readonly State state;

    public StateTests()
    {
        state = new State
        {
            OriginContentFolder = "folder",
            CoverFilePath = "cover.png",
            ZipFilePath = "file.zip",
            PdfFilePath = "file.pdf",
            BloggerImageAsBase64 = "base64",
            BoxPdfReaderKey = "key",
            PingText = "@everyone",
            MegaZipLink = "mega-zip",
            MegaPdfLink = "mega-pdf",
            DriveZipLink = "drive-zip",
            DrivePdfLink = "drive-pdf",
            BoxZipLink = "box-zip",
            BoxPdfLink = "box-pdf",
            MangaDexLink = "mangadex",
            SakuraMangasLink = "sakura",
            BloggerLink = "blogger"
        };
    }

    public class MergeWith : StateTests
    {
        [Fact]
        public void GivenParallelResultsShouldMergeNonNullLinkProperties()
        {
            var baseState = new State { MegaZipLink = "https://mega.nz/zip" };
            var other = new State { BoxZipLink = "https://box.com/zip", Steps = new EnabledSteps([]) };

            var merged = baseState.MergeWith(other);

            merged.MegaZipLink.Should().Be("https://mega.nz/zip");
            merged.BoxZipLink.Should().Be("https://box.com/zip");
        }

        [Fact]
        public void GivenParallelConversionResultsShouldMergeFilePaths()
        {
            var baseState = new State { ZipFilePath = "/tmp/chapter.zip" };
            var other = new State { PdfFilePath = "/tmp/chapter.pdf", Steps = new EnabledSteps([]) };

            var merged = baseState.MergeWith(other);

            merged.ZipFilePath.Should().Be("/tmp/chapter.zip");
            merged.PdfFilePath.Should().Be("/tmp/chapter.pdf");
        }

        [Fact]
        public void GivenUpdatedLinkShouldOverrideBaseLink()
        {
            var baseState = new State { MegaZipLink = "https://old.link" };
            var other = new State { MegaZipLink = "https://new.link", Steps = new EnabledSteps([]) };

            var merged = baseState.MergeWith(other);

            merged.MegaZipLink.Should().Be("https://new.link");
        }

        [Fact]
        public void GivenNullUpdatedLinkShouldPreserveBaseLink()
        {
            var baseState = new State { DriveZipLink = "https://drive.google.com/zip" };
            var other = new State { DriveZipLink = null, Steps = new EnabledSteps([]) };

            var merged = baseState.MergeWith(other);

            merged.DriveZipLink.Should().Be("https://drive.google.com/zip");
        }

        [Fact]
        public void GivenParallelSnapshotsShouldPreserveStepInfoFromBothSnapshots()
        {
            var stepA = A.Fake<IConversionStep>();
            var stepB = A.Fake<IConversionStep>();

            var baseState = new State
            {
                Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
                {
                    { stepA, new StepInfo(stepA) { Status = StepStatus.Success } },
                    { stepB, new StepInfo(stepB) { Status = StepStatus.QueuedForExecution } },
                })
            };
            var other = new State
            {
                Steps = new EnabledSteps(new Dictionary<IStep, StepInfo>
                {
                    { stepA, new StepInfo(stepA) { Status = StepStatus.QueuedForExecution } },
                    { stepB, new StepInfo(stepB) { Status = StepStatus.Success } },
                })
            };

            var merged = baseState.MergeWith(other);

            using var _ = new AssertionScope();
            merged.Steps[stepA].Status.Should().Be(StepStatus.Success);
            merged.Steps[stepB].Status.Should().Be(StepStatus.Success);
        }

        [Fact]
        public void GivenUpdatedStepInfoWithDifferentStatusShouldOverrideBaseStepInfo()
        {
            var step = A.Fake<IConversionStep>();
            var baseStepInfo = new StepInfo(step) { Status = StepStatus.QueuedForExecution };
            var baseEnabledSteps = new EnabledSteps(new() { { step, baseStepInfo } });
            var baseState = new State { Steps = baseEnabledSteps };

            var newStepInfo = new StepInfo(step) { Status = StepStatus.Success };
            var newEnabledSteps = new EnabledSteps(new() { { step, newStepInfo } });
            var newState = new State { Steps = newEnabledSteps };

            var merged = baseState.MergeWith(newState);

            merged.Steps[step].Status.Should().Be(StepStatus.Success);
        }
    }
}
