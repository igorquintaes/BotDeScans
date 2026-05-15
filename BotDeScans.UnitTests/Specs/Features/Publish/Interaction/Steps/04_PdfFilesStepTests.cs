using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class PdfFilesStepTests : UnitTest
{
    private readonly PdfFilesStep step;
    private readonly State state;

    public PdfFilesStepTests()
    {
        fixture.FreezeFake<FileService>();
        fixture.FreezeFake<FileReleaseService>();

        state = new State
        {
            ChapterInfo = fixture.Create<Info>(),
            OriginContentFolder = fixture.Create<string>()
        };

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
        private readonly string pdfPath;

        public ExecuteAsync()
        {
            var scopedDirectory = fixture.Create<string>();
            pdfPath = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .CreateScopedDirectory())
                .Returns(scopedDirectory);

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreatePdfFileAsync(
                    state.ChapterInfo.ChapterNumber,
                    state.OriginContentFolder,
                    scopedDirectory))
                .Returns(Result.Ok(pdfPath));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetPdfFilePath()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.PdfFilePath.Should().Be(pdfPath);
        }

        [Fact]
        public async Task GivenErrorToCreazePdfShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreatePdfFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
