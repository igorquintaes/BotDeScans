using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.CapivaraTradutor;
using BotDeScans.App.Services.CapivaraTradutor.InternalServices;
using BotDeScans.UnitTests.FakeObjects;
using FluentResults;
using System.Collections.Immutable;
using System.Net;
using static BotDeScans.App.Services.CapivaraTradutor.CapivaraTradutorService;
using static BotDeScans.App.Services.CapivaraTradutor.InternalServices.CapivaraTradutorUploadService;

namespace BotDeScans.UnitTests.Specs.Services.CapivaraTradutor;

public class CapivaraTradutorServiceTests : UnitTest
{
    private readonly CapivaraTradutorService service;

    public CapivaraTradutorServiceTests()
    {
        fixture.Inject(new HttpClient(fixture.FreezeFake<FakeHttpMessageHandler>()));
        fixture.FreezeFake<CapivaraTradutorUploadService>();

        service = fixture.Create<CapivaraTradutorService>();
    }

    public class UploadAsync : CapivaraTradutorServiceTests
    {
        private readonly string titleId;
        private readonly Work work;
        private readonly string filesDirectory;
        private readonly string chapterUrl;

        public UploadAsync()
        {
            titleId = fixture.Create<string>();
            work = fixture.Create<Work>();
            filesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-files-upload");
            if (Directory.Exists(filesDirectory))
                Directory.Delete(filesDirectory, true);

            Directory.CreateDirectory(filesDirectory);
            File.Create(Path.Combine(filesDirectory, "1.png")).Dispose();
            
            chapterUrl = fixture.Create<string>();

            var uploadData = ImmutableArray.Create(
                new UploadData(
                    "https://example.com/upload1",
                    "key1",
                    "path1"));

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .RequestForUploadAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<string>>.Ignored,
                    cancellationToken))
                .Returns(Result.Ok(uploadData));

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .UploadFilesAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<UploadData>>.Ignored,
                    cancellationToken))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .CommitAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<UploadData>>.Ignored,
                    A<Info>.Ignored,
                    A<Work>.Ignored,
                    cancellationToken))
                .Returns(Result.Ok(chapterUrl));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.UploadAsync(
                fixture.Freeze<Info>(),
                work,
                filesDirectory,
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorToRequestForUploadShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .RequestForUploadAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<string>>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(
                fixture.Freeze<Info>(),
                work,
                filesDirectory,
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToUploadFilesShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .UploadFilesAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<UploadData>>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(
                fixture.Freeze<Info>(),
                work,
                filesDirectory,
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCommitShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .CommitAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<UploadData>>.Ignored,
                    A<Info>.Ignored,
                    A<Work>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(
                fixture.Freeze<Info>(),
                work,
                filesDirectory,
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenMultipleSuccessStepsShouldAggregateReasons()
        {
            const string INFO_MESSAGE1 = "Upload request completed";
            const string INFO_MESSAGE2 = "Files uploaded successfully";

            var uploadData = ImmutableArray.Create(
                new UploadData(
                    "https://example.com/upload1",
                    "key1",
                    "path1"));

            var resultWithData1 = Result.Ok(uploadData).WithSuccess(INFO_MESSAGE1);
            var resultWithData2 = Result.Ok().WithSuccess(INFO_MESSAGE2);

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .RequestForUploadAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<string>>.Ignored,
                    cancellationToken))
                .Returns(resultWithData1);

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .UploadFilesAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<UploadData>>.Ignored,
                    cancellationToken))
                .Returns(resultWithData2);

            var result = await service.UploadAsync(
                fixture.Freeze<Info>(),
                work,
                filesDirectory,
                cancellationToken);

            result.Should().BeSuccess();
            result.Reasons.Should().Contain(r => r.Message == INFO_MESSAGE1);
            result.Reasons.Should().Contain(r => r.Message == INFO_MESSAGE2);
        }

        [Fact]
        public async Task GivenMixedSuccessAndErrorShouldAggregateAllReasons()
        {
            const string ERROR_MESSAGE = "Upload failed";
            const string INFO_MESSAGE = "Request completed";

            var uploadData = ImmutableArray.Create(
                new UploadData(
                    "https://example.com/upload1",
                    "key1",
                    "path1"));

            var resultWithData = Result.Ok(uploadData).WithSuccess(INFO_MESSAGE);

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .RequestForUploadAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<string>>.Ignored,
                    cancellationToken))
                .Returns(resultWithData);

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorUploadService>()
                .UploadFilesAsync(
                    A<HttpClient>.Ignored,
                    A<IEnumerable<UploadData>>.Ignored,
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.UploadAsync(
                fixture.Freeze<Info>(),
                work,
                filesDirectory,
                cancellationToken);

            result.Should().BeFailure();
            result.Reasons.Should().Contain(r => r.Message == INFO_MESSAGE);
            result.Reasons.Should().Contain(r => r.Message == ERROR_MESSAGE);
        }
    }

    public class PingCredentialsAsync : CapivaraTradutorServiceTests
    {
        [Fact]
        public async Task GivenStatusCodeErrorShouldReturnFailResult()
        {
            const string RESPONSE_CONTENT = "anything";

            using var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(RESPONSE_CONTENT)
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(errorResponse);

            var result = await service.PingCredentialsAsync(cancellationToken);

            result.Should()
                .BeFailure().And
                .HaveError("Erro ao se comunicar com o Capivara Tradutor. Cheque o arquivo de logs para mais detalhes.");

            result.Errors.Should()
                .ContainSingle().Which.Reasons.Should()
                .ContainSingle().Which.Message.Should()
                .Be(RESPONSE_CONTENT);
        }

        [Fact]
        public async Task GivenStatusCodeOKAndStatusTrueShouldReturnSuccessResult()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{ ""status"": true }")
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(response);

            var result = await service.PingCredentialsAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenStatusCodeOKAndStatusFalseShouldReturnFailResult()
        {
            const string RESPONSE_CONTENT = @"{ ""status"": false }";

            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RESPONSE_CONTENT)
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(response);

            var result = await service.PingCredentialsAsync(cancellationToken);

            result.Should()
                .BeFailure().And
                .HaveError("Erro ao se comunicar com o Capivara Tradutor. Cheque o arquivo de logs para mais detalhes.");

            result.Errors.Should()
                .ContainSingle().Which.Reasons.Should()
                .ContainSingle().Which.Message.Should()
                .Be(RESPONSE_CONTENT);
        }
    }
}
