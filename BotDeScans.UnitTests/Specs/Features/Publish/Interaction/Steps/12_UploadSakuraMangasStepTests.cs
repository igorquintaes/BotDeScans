using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadSakuraMangasStepTests : UnitTest
{
    private readonly UploadSakuraMangasStep step;

    public UploadSakuraMangasStepTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<SakuraMangasService>();

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
    }

    public class ValidateAsync : UploadSakuraMangasStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadSakuraMangasStepTests
    {
        private readonly string sakuraMangasLink;

        public ExecuteAsync()
        {
            var state = fixture.Freeze<State>();
            state.Title.References.Clear();
            state.Title.References.Add(new TitleReference
            {
                Key = ExternalReference.SakuraMangas,
                Value = fixture.Create<string>(),
                Title = state.Title
            });

            sakuraMangasLink = fixture.Create<string>();
            A.CallTo(() => fixture
                .FreezeFake<SakuraMangasService>()
                .UploadAsync(
                    state.ChapterInfo.ChapterNumber,
                    state.ChapterInfo.ChapterName,
                    state.Title.References.Single().Value,
                    state.InternalData.ZipFilePath!,
                    cancellationToken))
                .Returns(Result.Ok(sakuraMangasLink));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSaveSakuraMangasLinkInState()
        {
            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<State>().ReleaseLinks.SakuraMangas.Should().Be(sakuraMangasLink);
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

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
