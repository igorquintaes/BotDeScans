using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.MangaDex;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class UploadMangDexStepTests : UnitTest
{
    private readonly UploadMangaDexStep step;

    public UploadMangDexStepTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<IConfiguration>();
        fixture.FreezeFake<MangaDexService>();

        step = fixture.Create<UploadMangaDexStep>();
    }

    public class Properties : UploadMangDexStepTests
    {
        [Fact]
        public void ShouldHaveExpectedType() =>
            step.Type.Should().Be(StepType.Upload);

        [Fact]
        public void ShouldHaveExpectedName() =>
            step.Name.Should().Be(StepName.UploadMangadex);

        [Fact]
        public void ShouldHaveExpectedDependency() =>
            step.Dependency.Should().Be(StepName.ZipFiles);
    }

    public class ValidateAsync : UploadMangDexStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAsync(cancellationToken);

            result.Should().BeSuccess();
        }
    }

    public class ExecuteAsync : UploadMangDexStepTests
    {
        private const string CHAPTER_ID = "random-value";

        public ExecuteAsync()
        {
            fixture.Freeze<PublishState>().Title = fixture
                .Build<Title>()
                .With(x => x.References, fixture
                    .Build<TitleReference>()
                    .With(x => x.Key, ExternalReference.MangaDex)
                    .CreateMany(1)
                    .ToList())
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<MangaDexService>()
                .UploadAsync(
                    fixture.Freeze<PublishState>().ChapterInfo,
                    fixture.Freeze<PublishState>().Title.References.Single().Value,
                    fixture.Freeze<PublishState>().InternalData.OriginContentFolder,
                    cancellationToken))
                .Returns(Result.Ok(new Chapter() { Id = CHAPTER_ID }));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldSetMangadexLinkStateValue()
        {
            var expectedLink = $"https://mangadex.org/chapter/{CHAPTER_ID}/1";
            fixture.Freeze<PublishState>().ReleaseLinks.MangaDex = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().ReleaseLinks.MangaDex.Should().Be(expectedLink);
        }

        [Fact]
        public async Task GivenErrorToUploadChapterShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexService>()
                .UploadAsync(
                    A<Info>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
