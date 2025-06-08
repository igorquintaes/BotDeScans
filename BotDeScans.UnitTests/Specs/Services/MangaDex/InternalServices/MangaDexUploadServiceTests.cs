using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.MangaDex.InternalServices;
using MangaDexSharp;
using MangaDexSharp.Utilities.Upload;
using static MangaDexSharp.UploadSessionFile;

namespace BotDeScans.UnitTests.Specs.Services.MangaDex.InternalServices;

public class MangaDexUploadServiceTests : UnitTest
{
    private readonly MangaDexUploadService service;

    public MangaDexUploadServiceTests()
    {
        fixture.Freeze<MangaDexAccessToken>();
        fixture.FreezeFake<IUploadUtilityService>();
        fixture.FreezeFake<IMangaDex>();

        A.CallTo(() => fixture
            .FreezeFake<IMangaDex>().Upload)
            .Returns(fixture.FreezeFake<IMangaDexUploadService>());

        service = fixture.Create<MangaDexUploadService>();
    }

    public class GetOpenSessionAsync : MangaDexUploadServiceTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var uploadSession = fixture.Create<UploadSession>();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Get(default, fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(new MangaDexRoot<UploadSession> { Data = uploadSession });

            var result = await service.GetOpenSessionAsync();

            result.Should().BeSuccess().And.HaveValue(uploadSession);
        }

        [Fact]
        public async Task GivenNotFoundErrorShouldReturnSuccessResult()
        {
            const int NOT_FOUND = 404;
            var uploadSession = fixture.Create<UploadSession>();
            var root = new MangaDexRoot<UploadSession>
            {
                Data = uploadSession,
                Errors = [new MangaDexError { Status = NOT_FOUND }]
            };

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Get(default, fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(root);

            var result = await service.GetOpenSessionAsync();

            result.Should().BeSuccess().And.HaveValue(uploadSession);
        }

        [Fact]
        public async Task GivenUnexpectedErrorShouldReturnSuccessResult()
        {
            const int ERROR = 500;
            var uploadSession = fixture.Create<UploadSession>();
            var root = new MangaDexRoot<UploadSession>
            {
                Data = uploadSession,
                Errors = [new MangaDexError { Status = ERROR, Title = "some-title", Detail = "some-detail", Id = "some-id" }]
            };

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Get(default, fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(root);

            var result = await service.GetOpenSessionAsync();

            result.Should().BeFailure().And.HaveError("500 - some-title - some-detail");
        }
    }

    public class AbandonSessionAsync : MangaDexUploadServiceTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var sessionId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Abandon(sessionId, fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(new MangaDexRoot());

            var result = await service.AbandonSessionAsync(sessionId);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenUnexpectedErrorShouldReturnSuccessResult()
        {
            const int ERROR = 500;
            var sessionId = fixture.Create<string>();
            var root = new MangaDexRoot<UploadSession>
            {
                Errors = [new MangaDexError { Status = ERROR, Title = "some-title", Detail = "some-detail", Id = "some-id" }]
            };

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Abandon(sessionId, fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(root);

            var result = await service.AbandonSessionAsync(sessionId);

            result.Should().BeFailure().And.HaveError("500 - some-title - some-detail");
        }
    }

    public class UploadFilesAsync : MangaDexUploadServiceTests
    {
        private static readonly string filesPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "files");

        private readonly string titleId;
        private readonly string groupId;

        public UploadFilesAsync()
        {
            if (Directory.Exists(filesPath))
                Directory.Delete(filesPath, true);

            Directory.CreateDirectory(filesPath);
            File.Create(Path.Combine(filesPath, "1.png")).Dispose();
            File.Create(Path.Combine(filesPath, "2.png")).Dispose();
            File.Create(Path.Combine(filesPath, "3.png")).Dispose();

            titleId = fixture.Create<string>();
            groupId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<IUploadUtilityService>()
                .New(
                    titleId,
                    A<string[]>.That.Matches(x => x.Count() == 1 && x[0] == groupId),
                    A<Action<IUploadSettings>>.Ignored))
                .Returns(fixture.FreezeFake<IUploadInstance>());

            A.CallTo(() => fixture
                .FreezeFake<IUploadInstance>()
                .Commit(
                    A<ChapterDraft>.That.Matches(x =>
                        x.Chapter == fixture.Freeze<Info>().ChapterNumber &&
                        x.Volume == fixture.Freeze<Info>().ChapterVolume &&
                        x.Title == fixture.Freeze<Info>().ChapterName &&
                        x.TranslatedLanguage == fixture.Freeze<Info>().Language),
                    default))
                .Returns(fixture.Freeze<Chapter>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnExpectedChapter()
        {
            var chapter = await service.UploadFilesAsync(
                filesPath,
                titleId,
                groupId,
                fixture.Freeze<Info>(),
                cancellationToken);

            chapter.Should().Be(fixture.Freeze<Chapter>());
        }

        [Fact]
        public async Task GivenDirectoryShouldUploadAllFilesInside()
        {
            await service.UploadFilesAsync(
                filesPath,
                titleId,
                groupId,
                fixture.Freeze<Info>(),
                cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IUploadInstance>()
                .UploadFile(Path.Combine(filesPath, "1.png"), default))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<IUploadInstance>()
                .UploadFile(Path.Combine(filesPath, "2.png"), default))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<IUploadInstance>()
                .UploadFile(Path.Combine(filesPath, "3.png"), default))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task ShouldBuildExpectedUploadSettings()
        {
            // Arrange
            UploadSessionFile[] orderedUploadSessionFiles = [];

            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithAuthToken(A<string>.Ignored))
                .Returns(fixture.FreezeFake<IUploadSettings>());

            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithCancellationToken(A<CancellationToken>.Ignored))
                .Returns(fixture.FreezeFake<IUploadSettings>());

            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithMaxBatchSize(A<int>.Ignored))
                .Returns(fixture.FreezeFake<IUploadSettings>());

            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithPageOrderFactory(A<Func<IEnumerable<UploadSessionFile>, IOrderedEnumerable<UploadSessionFile>>>.Ignored))
                .Returns(fixture.FreezeFake<IUploadSettings>());

            A.CallTo(() => fixture
                .FreezeFake<IUploadUtilityService>()
                .New(A<string>.Ignored,
                     A<string[]>.Ignored,
                     A<Action<IUploadSettings>>.Ignored))
                .Invokes((string _, string[] _, Action<IUploadSettings> action) =>
                     action.Invoke(fixture.FreezeFake<IUploadSettings>()));

            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithPageOrderFactory(A<Func<IEnumerable<UploadSessionFile>, IOrderedEnumerable<UploadSessionFile>>>.Ignored))
                .Invokes((Func<IEnumerable<UploadSessionFile>, IOrderedEnumerable<UploadSessionFile>> factory) =>
                {
                    var randomSessionFiles = new List<UploadSessionFile>()
                    {
                        new() { Attributes = new UploadSessionFileAttributesModel { OriginalFileName = "02.png" } },
                        new() { Attributes = new UploadSessionFileAttributesModel { OriginalFileName = "01.png" } },
                        new() { Attributes = new UploadSessionFileAttributesModel { OriginalFileName = "cover.png" } },
                        new() { Attributes = new UploadSessionFileAttributesModel { OriginalFileName = "03.png" } }
                    };

                    orderedUploadSessionFiles = factory(randomSessionFiles).ToArray();
                });

            // Act
            await service.UploadFilesAsync(
                filesPath,
                titleId,
                groupId,
                fixture.Freeze<Info>(),
                cancellationToken);

            // Assert
            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithAuthToken(fixture.Freeze<MangaDexAccessToken>().Value))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithCancellationToken(cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithMaxBatchSize(10))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<IUploadSettings>()
                .WithPageOrderFactory(A<Func<IEnumerable<UploadSessionFile>, IOrderedEnumerable<UploadSessionFile>>>.Ignored))
                .MustHaveHappenedOnceExactly();

            orderedUploadSessionFiles[0].Attributes!.OriginalFileName.Should().Be("01.png");
            orderedUploadSessionFiles[1].Attributes!.OriginalFileName.Should().Be("02.png");
            orderedUploadSessionFiles[2].Attributes!.OriginalFileName.Should().Be("03.png");
            orderedUploadSessionFiles[3].Attributes!.OriginalFileName.Should().Be("cover.png");
        }
    }
}
