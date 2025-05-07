using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.MangaDex.InternalServices;
using FakeItEasy;
using FluentAssertions.Execution;
using FluentResults;
using MangaDexSharp;
using Microsoft.Testing.Platform.TestHost;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Remora.Discord.API.Objects;
using System.Text.RegularExpressions;

namespace BotDeScans.UnitTests.Specs.Services.MangaDex.InternalServices;

public class MangaDexUploadServiceTests : UnitTest
{
    private readonly MangaDexUploadService service;

    public MangaDexUploadServiceTests()
    {
        fixture.Freeze<MangaDexAccessToken>();
        fixture.FreezeFake<FileService>();
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
                .Get(fixture.Freeze<MangaDexAccessToken>().Value))
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
                .Get(fixture.Freeze<MangaDexAccessToken>().Value))
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
                .Get(fixture.Freeze<MangaDexAccessToken>().Value))
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

    public class CreateSessionAsync : MangaDexUploadServiceTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var titleId = fixture.Create<string>();
            var groupId = fixture.Create<string>();
            var uploadSession = fixture.Create<UploadSession>();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Begin(titleId, new[] { groupId }, fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(new MangaDexRoot<UploadSession> { Data = uploadSession });

            var result = await service.CreateSessionAsync(titleId, groupId);

            result.Should().BeSuccess().And.HaveValue(uploadSession);
        }

        [Fact]
        public async Task GivenUnexpectedErrorShouldReturnSuccessResult()
        {
            const int ERROR = 500;
            var titleId = fixture.Create<string>();
            var groupId = fixture.Create<string>();
            var uploadSession = fixture.Create<UploadSession>();

            var root = new MangaDexRoot<UploadSession>
            {
                Data = uploadSession,
                Errors = [new MangaDexError { Status = ERROR, Title = "some-title", Detail = "some-detail", Id = "some-id" }]
            };

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Begin(titleId, new[] { groupId }, fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(root);

            var result = await service.CreateSessionAsync(titleId, groupId);

            result.Should().BeFailure().And.HaveError("500 - some-title - some-detail");
        }
    }

    public class UploadFilesAsync : MangaDexUploadServiceTests
    {
        private static readonly string filesPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "files");

        private readonly string sessionId;

        public UploadFilesAsync()
        {
            const int MAX_CHUNK_FILES = 10;
            const long MAX_CHUNK_BYTES = 150 * 1024 * 1024;
            sessionId = fixture.Create<string>();

            if (Directory.Exists(filesPath))
                Directory.Delete(filesPath, true);

            Directory.CreateDirectory(filesPath);
            File.Create(Path.Combine(filesPath, "1.png")).Dispose();
            File.Create(Path.Combine(filesPath, "2.png")).Dispose();
            File.Create(Path.Combine(filesPath, "3.png")).Dispose();

            var chunk1 = new FileChunk();
            chunk1.Add("NAME-1", File.OpenRead(Path.Combine(filesPath, "1.png")));
            chunk1.Add("NAME-2", File.OpenRead(Path.Combine(filesPath, "2.png")));

            var chunk2 = new FileChunk();
            chunk2.Add("NAME-3", File.OpenRead(Path.Combine(filesPath, "3.png")));

            var chunks = new List<FileChunk> { chunk1, chunk2 };

            A.CallTo(() => fixture
                .FreezeFake<FileService>()
                .CreateChunks(
                    A<IOrderedEnumerable<string>>.That.Matches(files =>
                        files.Contains(Path.Combine(filesPath, "1.png")) &&
                        files.Contains(Path.Combine(filesPath, "2.png")) &&
                        files.Contains(Path.Combine(filesPath, "3.png")) &&
                        files.Count() == 3),
                    MAX_CHUNK_FILES,
                    MAX_CHUNK_BYTES))
                .Returns(chunks);

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Upload(
                    sessionId,
                    fixture.Freeze<MangaDexAccessToken>().Value,
                    cancellationToken,
                    A<StreamFileUpload[]>.That.Matches(files =>
                        files.Any(file => file.FileName == chunk1.Files.First().Key && file.Data == chunk1.Files.First().Value) &&
                        files.Any(file => file.FileName == chunk1.Files.Last().Key && file.Data == chunk1.Files.Last().Value) &&
                        files.Count() == 2)))
                .Returns(new UploadSessionFileList
                {
                    Data =
                    [
                        new UploadSessionFile { Id = "ID-1" },
                        new UploadSessionFile { Id = "ID-2" },
                    ]
                });

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Upload(
                    sessionId,
                    fixture.Freeze<MangaDexAccessToken>().Value,
                    cancellationToken,
                    A<StreamFileUpload[]>.That.Matches(files =>
                        files.Any(file => file.FileName == chunk2.Files.First().Key && file.Data == chunk2.Files.First().Value) &&
                        files.Count() == 1)))
                .Returns(new UploadSessionFileList
                {
                    Data =
                    [
                        new UploadSessionFile { Id = "ID-3" },
                    ]
                });

        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.UploadFilesAsync(filesPath, sessionId, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            result.ValueOrDefault.Should().BeEquivalentTo(["ID-1", "ID-2", "ID-3"], options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task GivenExecutionErrorShouldReturnFailResultAndStopUpload()
        {
            const int ERROR = 500;
            var root = new UploadSessionFileList
            {
                Errors = [new MangaDexError { Status = ERROR, Title = "some-title", Detail = "some-detail", Id = "some-id" }]
            };

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Upload(
                    sessionId,
                    fixture.Freeze<MangaDexAccessToken>().Value,
                    cancellationToken,
                    A<StreamFileUpload[]>.Ignored))
                .Returns(root);

            var result = await service.UploadFilesAsync(filesPath, sessionId, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeFailure().And.HaveError("500 - some-title - some-detail");

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Upload(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    cancellationToken,
                    A<StreamFileUpload[]>.Ignored))
                .MustHaveHappenedOnceExactly();
        }
    }

    public class CommitSessionAsync : MangaDexUploadServiceTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var sessionId = fixture.Create<string>();
            var chapterName = fixture.Create<string>();
            var chapterNumber = fixture.Create<string>();
            var volume = fixture.Create<string>();
            var pagesIds = fixture.CreateMany<string>().ToArray();
            var chapter = fixture.Create<Chapter>();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Commit(
                    sessionId, 
                    A<UploadSessionCommit>.That.Matches(data =>
                        data.Chapter.Chapter == chapterNumber &&
                        data.Chapter.Volume == volume &&
                        data.Chapter.Title == chapterName &&
                        data.Chapter.TranslatedLanguage == "pt-br" &&
                        data.PageOrder == pagesIds),
                    fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(new MangaDexRoot<Chapter> { Data = chapter });

            var result = await service.CommitSessionAsync(sessionId, chapterName, chapterNumber, volume, pagesIds);

            result.Should().BeSuccess().And.HaveValue(chapter);
        }

        [Fact]
        public async Task GivenUnexpectedErrorShouldReturnSuccessResult()
        {
            const int ERROR = 500;
            var sessionId = fixture.Create<string>();
            var chapterName = fixture.Create<string>();
            var chapterNumber = fixture.Create<string>();
            var volume = fixture.Create<string>();
            var pagesIds = fixture.CreateMany<string>().ToArray();
            var chapter = fixture.Create<Chapter>();

            var root = new MangaDexRoot<Chapter>
            {
                Data = chapter,
                Errors = [new MangaDexError { Status = ERROR, Title = "some-title", Detail = "some-detail", Id = "some-id" }]
            };

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Commit(
                    sessionId,
                    A<UploadSessionCommit>.That.Matches(data =>
                        data.Chapter.Chapter == chapterNumber &&
                        data.Chapter.Volume == volume &&
                        data.Chapter.Title == chapterName &&
                        data.Chapter.TranslatedLanguage == "pt-br" &&
                        data.PageOrder == pagesIds),
                    fixture.Freeze<MangaDexAccessToken>().Value))
                .Returns(root);

            var result = await service.CommitSessionAsync(sessionId, chapterName, chapterNumber, volume, pagesIds);

            result.Should().BeFailure().And.HaveError("500 - some-title - some-detail");
        }
    }
}
