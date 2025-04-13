using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Models;
using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Steps;

public class UploadMangDexStepTests : UnitTest
{
    private readonly IStep step;

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
        public void ShouldHaveExpectedName() =>
            step.StepName.Should().Be(StepEnum.UploadMangadex);

        [Fact]
        public void ShouldHaveExpectedType() =>
            step.StepType.Should().Be(StepType.Upload);
    }

    public class ValidateBeforeFilesManagementAsync : UploadMangDexStepTests
    {
        public ValidateBeforeFilesManagementAsync()
        {
            fixture.Freeze<PublishState>().Title = fixture
                .Build<Title>()
                .With(x => x.References, fixture
                    .Build<TitleReference>()
                    .With(x => x.Key, ExternalReference.MangaDex)
                    .CreateMany(1)
                    .ToList())
                .Create();

            fixture.FreezeFakeConfiguration("Mangadex:Username", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Mangadex:Password", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Mangadex:ClientId", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Mangadex:ClientSecret", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Mangadex:GroupId", fixture.Create<string>());
        }

        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenMultipleTitleReferencesButIncluedMangaDexShouldReturnSuccess()
        {
            fixture.Freeze<PublishState>().Title = fixture
                .Build<Title>()
                .With(x => x.References,
                [
                    fixture.Build<TitleReference>()
                           .With(x => x.Key, (ExternalReference)999)
                           .Create(),
                    fixture.Build<TitleReference>()
                           .With(x => x.Key, ExternalReference.MangaDex)
                           .Create(),
                    fixture.Build<TitleReference>()
                           .With(x => x.Key, (ExternalReference)888)
                           .Create()
                ])
                .Create();

            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenNoneMangaDexTitleReferenceShoultReturnFailResult()
        {
            fixture.Freeze<PublishState>().Title = fixture
                .Build<Title>()
                .With(x => x.References,
                [
                    fixture.Build<TitleReference>()
                           .With(x => x.Key, (ExternalReference)999)
                           .Create()
                ])
                .Create();

            var result = await step.ValidateBeforeFilesManagementAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError("Não foi definido uma referência para a publicação da obra na MangaDex.");
        }
    }

    public class ValidateAfterFilesManagementAsync : UploadMangDexStepTests
    {
        [Fact]
        public async Task ShouldReturnSuccess()
        {
            var result = await step.ValidateAfterFilesManagementAsync(cancellationToken);

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
                .ClearPendingUploadsAsync())
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<MangaDexService>()
                .UploadChapterAsync(
                    fixture.Freeze<PublishState>().Title.References.Single().Value,
                    fixture.Freeze<PublishState>().ReleaseInfo.ChapterName,
                    fixture.Freeze<PublishState>().ReleaseInfo.ChapterNumber,
                    fixture.Freeze<PublishState>().ReleaseInfo.ChapterVolume,
                    fixture.Freeze<PublishState>().InternalData.OriginContentFolder, 
                    cancellationToken))
                .Returns(Result.Ok(CHAPTER_ID));
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
            fixture.Freeze<PublishState>().ReleaseLinks.MangaDexLink = null!;

            await step.ExecuteAsync(cancellationToken);

            fixture.Freeze<PublishState>().ReleaseLinks.MangaDexLink.Should().Be(expectedLink);
        }

        [Fact]
        public async Task GivenErrorToClearPendingUploadsShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexService>()
                .ClearPendingUploadsAsync())
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToUploadChapterShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexService>()
                .UploadChapterAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await step.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
