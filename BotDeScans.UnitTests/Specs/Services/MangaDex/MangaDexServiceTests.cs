using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Services.MangaDex;
using BotDeScans.App.Services.MangaDex.InternalServices;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.UnitTests.Specs.Services.MangaDex;

public class MangaDexServiceTests : UnitTest
{
    private readonly MangaDexService service;

    public MangaDexServiceTests()
    {
        fixture.FreezeFake<MangaDexUploadService>();
        fixture.FreezeFake<IConfiguration>();

        service = fixture.Create<MangaDexService>();
    }

    public class UploadAsync : MangaDexServiceTests
    {
        private readonly Info info;
        private readonly string titleId;
        private readonly string filesDirectory;

        private readonly Chapter commitValue;

        public UploadAsync()
        {
            info = fixture.Create<Info>();
            titleId = fixture.Create<string>();
            filesDirectory = fixture.Create<string>();

            var groupId = fixture.Create<string>();
            fixture.FreezeFakeConfiguration("Mangadex:GroupId", groupId);

            var sessionId = fixture.Create<string>();
            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .CreateSessionAsync(titleId, groupId))
                .Returns(Result.Ok(new UploadSession { Id = sessionId }));

            var uploadsIds = fixture.CreateMany<string>().ToArray();
            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .UploadFilesAsync(
                    filesDirectory,
                    sessionId,
                    cancellationToken))
                .Returns(Result.Ok(uploadsIds));

            commitValue = fixture.Create<Chapter>();
            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .CommitSessionAsync(
                    sessionId,
                    info.ChapterName,
                    info.ChapterNumber,
                    info.ChapterVolume,
                    uploadsIds))
                .Returns(Result.Ok(commitValue));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.UploadAsync(info, titleId, filesDirectory, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(commitValue);
        }

        [Fact]
        public async Task GivenExistingSessionShouldCallAbandonSession()
        {
            var existingSessionId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .GetOpenSessionAsync())
                .Returns(Result.Ok(new UploadSession { Id = existingSessionId }));

            await service.UploadAsync(info, titleId, filesDirectory, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .AbandonSessionAsync(existingSessionId))
                .MustHaveHappenedOnceExactly();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GivenNoExistingSessionShouldNotCallAbandonSession(string? existingSessionId)
        {
            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .GetOpenSessionAsync())
                .Returns(Result.Ok(new UploadSession { Id = existingSessionId! }));

            await service.UploadAsync(info, titleId, filesDirectory, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .AbandonSessionAsync(A<string>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorToGetOpenSessionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .GetOpenSessionAsync())
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(info, titleId, filesDirectory, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToAbandonSessionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .GetOpenSessionAsync())
                .Returns(Result.Ok(new UploadSession { Id = fixture.Create<string>() }));

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .AbandonSessionAsync(A<string>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(info, titleId, filesDirectory, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCreateSessionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .CreateSessionAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(info, titleId, filesDirectory, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToUploadFilesShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .UploadFilesAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(info, titleId, filesDirectory, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCommitSessionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .CommitSessionAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string[]>.Ignored))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(info, titleId, filesDirectory, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
