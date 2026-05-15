using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadSakuraMangasStepTests : UnitTest
{
    private readonly UploadSakuraMangasStep step;
    private readonly State state;

    public UploadSakuraMangasStepTests()
    {
        fixture.FreezeFake<SakuraMangasService>();

        var titleReference = new TitleReference
        {
            Key = ExternalReference.SakuraMangas,
            Value = fixture.Create<string>()
        };

        var title = fixture
            .Build<Title>()
            .With(x => x.References, [titleReference])
            .Create();

        state = new State
        {
            Title = title,
            ChapterInfo = fixture.Create<Info>(),
            ZipFilePath = fixture.Create<string>()
        };

        step = fixture.Create<UploadSakuraMangasStep>();
    }

    public class Properties : UploadSakuraMangasStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Upload);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadSakuraMangas);

        [Fact]
        public void ShouldHaveExpectedDependency() =>
            step.Dependency.Should().Be(StepName.ZipFiles);

        [Fact]
        public void ShouldHaveExpectedContinueOnError() =>
            step.ContinueOnError.Should().BeTrue();
    }

    public class ValidateAsync : UploadSakuraMangasStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadSakuraMangasStepTests
    {
        private readonly string sakuraMangasLink;

        public ExecuteAsync()
        {
            sakuraMangasLink = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<SakuraMangasService>()
                .UploadAsync(
                    state.ChapterInfo.ChapterNumber,
                    state.ChapterInfo.ChapterName,
                    state.Title.References.Single().Value,
                    state.ZipFilePath!,
                    cancellationToken))
                .Returns(Result.Ok(sakuraMangasLink));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSaveSakuraMangasLinkInContext()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.SakuraMangasLink.Should().Be(sakuraMangasLink);
        }

        [Fact]
        public async Task GivenErrorToUploadFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<SakuraMangasService>()
                .UploadAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
