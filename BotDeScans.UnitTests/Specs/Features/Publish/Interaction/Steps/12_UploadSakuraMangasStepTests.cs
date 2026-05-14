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

    public UploadSakuraMangasStepTests()
    {
        fixture.FreezeFake<IPublishContext>();
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

        [Fact]
        public void ShouldHaveExpectedContinueOnError() =>
            step.ContinueOnError.Should().BeTrue();
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
            sakuraMangasLink = fixture.Create<string>();

            var titleReference = new TitleReference
            {
                Key = ExternalReference.SakuraMangas,
                Value = fixture.Create<string>()
            };

            var chapterInfo = fixture.Create<Info>();
            var zipPath = fixture.Create<string>();
            var title = fixture
                .Build<Title>()
                .With(x => x.References, [titleReference])
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>().ChapterInfo)
                .Returns(chapterInfo);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .ZipFilePath).Returns(zipPath);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>().Title)
                .Returns(title);

            A.CallTo(() => fixture
                .FreezeFake<SakuraMangasService>()
                .UploadAsync(
                    chapterInfo.ChapterNumber,
                    chapterInfo.ChapterName,
                    title.References.Single().Value,
                    zipPath,
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
        public async Task GivenSuccessfulExecutionShouldSaveSakuraMangasLinkInContext()
        {
            await step.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IPublishContext>()
                .SetSakuraMangasLink(sakuraMangasLink))
                .MustHaveHappenedOnceExactly();
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
