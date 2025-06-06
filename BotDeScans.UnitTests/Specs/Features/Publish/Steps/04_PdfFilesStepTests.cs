﻿using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class PdfFilesStepTests : UnitTest
{
    private readonly PdfFilesStep step;

    public PdfFilesStepTests()
    {
        fixture.Freeze<PublishState>();
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
                    fixture.Freeze<PublishState>().ChapterInfo.ChapterNumber,
                    fixture.Freeze<PublishState>().InternalData.OriginContentFolder,
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
            fixture.Freeze<PublishState>().InternalData.PdfFilePath = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().InternalData.PdfFilePath.Should().NotBeNullOrWhiteSpace();
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
