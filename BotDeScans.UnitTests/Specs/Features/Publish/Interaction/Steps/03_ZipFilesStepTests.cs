using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class ZipFilesStepTests : UnitTest
{
    private readonly ZipFilesStep step;
    private readonly State state;

    public ZipFilesStepTests()
    {
        fixture.FreezeFake<FileService>();
        fixture.FreezeFake<FileReleaseService>();

        state = new State
        {
            ChapterInfo = fixture.Create<Info>(),
            OriginContentFolder = fixture.Create<string>()
        };

        step = fixture.Create<ZipFilesStep>();
    }

    public class Properties : ZipFilesStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Conversion);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.ZipFiles);

        [Fact]
        public void ShouldHaveExpectedIsMandatory() =>
            step.IsMandatory.Should().Be(false);
    }

    public class ExecuteAsync : ZipFilesStepTests
    {
        private readonly string zipPath;

        public ExecuteAsync()
        {
            var scopedDirectory = fixture.Create<string>();
            zipPath = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FileReleaseService>()
                .CreateScopedDirectory())
                .Returns(scopedDirectory);

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreateZipFileAsync(
                    state.ChapterInfo.ChapterNumber,
                    state.OriginContentFolder,
                    scopedDirectory,
                    cancellationToken))
                .Returns(Result.Ok(zipPath));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetZipFilePath()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.ZipFilePath.Should().Be(zipPath);
        }

        [Fact]
        public async Task GivenErrorToCreazeZipShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreateZipFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
