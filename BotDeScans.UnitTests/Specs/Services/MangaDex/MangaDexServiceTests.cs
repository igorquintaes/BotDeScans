using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Services.MangaDex;
using BotDeScans.App.Services.MangaDex.InternalServices;
using FakeItEasy;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System.Text.RegularExpressions;

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

    public class UploadFilesAsync : MangaDexServiceTests
    {
        private readonly string titleId;
        private readonly string filesDirectory;
        private readonly string sessionId;

        public UploadFilesAsync()
        {
            titleId = fixture.Create<string>();
            filesDirectory = fixture.Create<string>();
            sessionId = fixture.Create<string>();
            var groupId = fixture.Create<string>();

            fixture.FreezeFakeConfiguration("Mangadex:GroupId", groupId);

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .GetOpenSessionAsync())
                .Returns(Result.Ok(new UploadSession { Id = sessionId }));

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .AbandonSessionAsync(sessionId))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .UploadFilesAsync(
                    filesDirectory,
                    titleId,
                    groupId,
                    fixture.Freeze<Info>(),
                    cancellationToken))
                .Returns(fixture.Freeze<Chapter>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.UploadAsync(fixture.Freeze<Info>(), titleId, filesDirectory, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<Chapter>());
        }

        [Fact]
        public async Task GivenExistingSessionShouldCallAbandonSession()
        {
            await service.UploadAsync(fixture.Freeze<Info>(), titleId, filesDirectory, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .AbandonSessionAsync(sessionId))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .UploadFilesAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<Info>.Ignored,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GivenNoneExistingSessionIdShouldNotCallAbandonSession(string? existingSessionId)
        {
            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .GetOpenSessionAsync())
                .Returns(Result.Ok(new UploadSession { Id = existingSessionId! }));

            await service.UploadAsync(fixture.Freeze<Info>(), titleId, filesDirectory, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .AbandonSessionAsync(A<string>.Ignored))
                .MustNotHaveHappened();

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .UploadFilesAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<Info>.Ignored,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenNoneExistingSessionShouldNotCallAbandonSession()
        {
            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .GetOpenSessionAsync())
                .Returns(Result.Ok(null as UploadSession)!);

            await service.UploadAsync(fixture.Freeze<Info>(), titleId, filesDirectory, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .AbandonSessionAsync(A<string>.Ignored))
                .MustNotHaveHappened();

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .UploadFilesAsync(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<Info>.Ignored,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToGetOpenSessionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .GetOpenSessionAsync())
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(fixture.Freeze<Info>(), titleId, filesDirectory, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToAbandonSessionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<MangaDexUploadService>()
                .AbandonSessionAsync(sessionId))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(fixture.Freeze<Info>(), titleId, filesDirectory, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
