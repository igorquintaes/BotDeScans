using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.CapivaraTradutor;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Steps;

public class UploadCapivaraTradutorStepTests : UnitTest
{
    private readonly UploadCapivaraTradutorStep step;
    private readonly State state;

    public UploadCapivaraTradutorStepTests()
    {
        fixture.FreezeFake<CapivaraTradutorService>();

        var titleReference = new TitleReference
        {
            Key = ExternalReference.CapivaraTradutor,
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
            OriginContentFolder = fixture.Create<string>()
        };

        step = fixture.Create<UploadCapivaraTradutorStep>();
    }

    public class Properties : UploadCapivaraTradutorStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() => step.Type.Should().Be(StepType.Upload);

        [Fact]
        public void ShouldHaveExpectedName() => step.Name.Should().Be(StepName.UploadCapivaraTradutor);

        [Fact]
        public void ShouldHaveExpectedDependency() => step.Dependency.Should().Be(StepName.ZipFiles);

        [Fact]
        public void ShouldHaveExpectedContinueOnError() => step.ContinueOnError.Should().BeTrue();
    }

    public class ValidateAsync : UploadCapivaraTradutorStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess() =>
            (await step.ValidateAsync(state, cancellationToken)).Should().BeSuccess();
    }

    public class ExecuteAsync : UploadCapivaraTradutorStepTests
    {
        private readonly string link;
        private readonly string referenceId;

        public ExecuteAsync()
        {
            link = fixture.Create<string>();
            referenceId = fixture.Create<string>();

            A.CallTo(() => fixture
                    .FreezeFake<CapivaraTradutorService>()
                    .UploadAsync(
                        state.ChapterInfo,
                        state.Title.References.Single(r => r.Key == ExternalReference.CapivaraTradutor).Value,
                        state.OriginContentFolder,
                        cancellationToken))
                .Returns(Result.Ok(link));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult() =>
            (await step.ExecuteAsync(state, cancellationToken)).Should().BeSuccess();

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSaveCapivaraTradutorLinkInContext()
        {
            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Value.CapivaraTradutorLink.Should().Be(link);
        }

        [Fact]
        public async Task GivenErrorToUploadFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                    .FreezeFake<CapivaraTradutorService>()
                    .UploadAsync(
                        A<Info>.Ignored,
                        A<string>.Ignored,
                        A<string>.Ignored,
                        cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(state, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
