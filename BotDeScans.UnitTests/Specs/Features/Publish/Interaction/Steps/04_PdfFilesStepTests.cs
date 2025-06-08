using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class PdfFilesStepTests : UnitTest
{
    private readonly PdfFilesStep step;

    public PdfFilesStepTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<FileService>();
        fixture.FreezeFake<FileReleaseService>();
        step = fixture.Create<PdfFilesStep>();
    }

    public class Properties : PdfFilesStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Management);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.PdfFiles);

        [Fact]
        public void ShouldHaveExpectedIsMandatory() =>
            step.IsMandatory.Should().Be(false);
    }

    public class ExecuteAsync : PdfFilesStepTests
    {
        public ExecuteAsync()
        {
            var scopedDirectory = fixture.Create<string>();
            var pdfDirectory = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .CreateScopedDirectory())
                .Returns(scopedDirectory);

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreatePdfFileAsync(
                    fixture.Freeze<State>().ChapterInfo.ChapterNumber,
                    fixture.Freeze<State>().InternalData.OriginContentFolder,
                    scopedDirectory))
                .Returns(Result.Ok(pdfDirectory));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetPdfFilePath()
        {
            fixture.Freeze<State>().InternalData.PdfFilePath = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().InternalData.PdfFilePath.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GivenErrorToCreazePdfShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreatePdfFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
