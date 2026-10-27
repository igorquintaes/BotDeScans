using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.CapivaraTradutor.InternalServices;
using BotDeScans.UnitTests.FakeObjects;
using FluentResults;
using System.Net;
using static BotDeScans.App.Services.CapivaraTradutor.CapivaraTradutorService;
using static BotDeScans.App.Services.CapivaraTradutor.InternalServices.CapivaraTradutorUploadService;

namespace BotDeScans.UnitTests.Specs.Services.CapivaraTradutor.InternalServices;

public class CapivaraTradutorUploadServiceTests : UnitTest
{
    private readonly CapivaraTradutorUploadService service;

    public CapivaraTradutorUploadServiceTests()
    {
        fixture.Inject(new HttpClient(fixture.FreezeFake<FakeHttpMessageHandler>()));
        fixture.FreezeFakeConfiguration("Capivara:GroupId", "group-slug");

        service = fixture.Create<CapivaraTradutorUploadService>();
    }

    public class RequestForUploadAsync : CapivaraTradutorUploadServiceTests
    {
        private readonly HttpResponseMessage httpResponse;
        private readonly string filesDirectory;

        public RequestForUploadAsync()
        {
            filesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-files");
            if (Directory.Exists(filesDirectory))
                Directory.Delete(filesDirectory, true);

            Directory.CreateDirectory(filesDirectory);
            File.Create(Path.Combine(filesDirectory, "1.png")).Dispose();
            File.Create(Path.Combine(filesDirectory, "2.png")).Dispose();

            httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(@"{
                    ""status"": true,
                    ""data"": {
                        ""uploads"": [
                            {
                                ""key"": ""tenants/group-slug/chapters/1791234567890_123456.webp"",
                                ""uploadUrl"": ""https://example.com/upload1""
                            },
                            {
                                ""key"": ""tenants/group-slug/chapters/1791234567890_123457.webp"",
                                ""uploadUrl"": ""https://example.com/upload2""
                            }
                        ]
                    }
                }")
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(httpResponse);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await service.RequestForUploadAsync(
                fixture.Freeze<HttpClient>(),
                Directory.GetFiles(filesDirectory).Order(),
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnUploadDataWithCorrectStructure()
        {
            var result = await service.RequestForUploadAsync(
                fixture.Freeze<HttpClient>(),
                Directory.GetFiles(filesDirectory).Order(),
                cancellationToken);

            result.Value.Should().HaveCount(2);
            result.Value[0].Key.Should().Be("tenants/group-slug/chapters/1791234567890_123456.webp");
            result.Value[0].UploadUrl.Should().Be("https://example.com/upload1");
            result.Value[1].Key.Should().Be("tenants/group-slug/chapters/1791234567890_123457.webp");
            result.Value[1].UploadUrl.Should().Be("https://example.com/upload2");
        }

        [Fact]
        public async Task GivenStatusCodeErrorShouldReturnFailResult()
        {
            using var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("anything")
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(errorResponse);

            var result = await service.RequestForUploadAsync(
                fixture.Freeze<HttpClient>(),
                Directory.GetFiles(filesDirectory).Order(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("anything");
        }

        [Fact]
        public async Task GivenBodyStatusFalseShouldReturnFailResult()
        {
            const string CONTENT = @"{ ""status"": false, ""data"": { ""uploads"": [] } }";
            using var errorResponse = new HttpResponseMessage()
            {
                Content = new StringContent(CONTENT)
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(errorResponse);

            var result = await service.RequestForUploadAsync(
                fixture.Freeze<HttpClient>(),
                Directory.GetFiles(filesDirectory).Order(),
                cancellationToken);

            result.Should().BeFailure().And
                .HaveError("Failed to upload files").Which
                .Errors.Single().Reasons.Should().ContainSingle().And
                .BeEquivalentTo([new Error(CONTENT)]);
        }
    }

    public class UploadFilesAsync : CapivaraTradutorUploadServiceTests
    {
        private readonly string filesDirectory;

        public UploadFilesAsync()
        {
            filesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-files-upload");
            if (Directory.Exists(filesDirectory))
                Directory.Delete(filesDirectory, true);
            Directory.CreateDirectory(filesDirectory);
            File.Create(Path.Combine(filesDirectory, "1.png")).Dispose();
            File.Create(Path.Combine(filesDirectory, "2.png")).Dispose();

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(new HttpResponseMessage(HttpStatusCode.OK));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var uploadData = new[]
            {
                new CapivaraTradutorUploadService.UploadData(
                    "https://example.com/upload1",
                    "key1",
                    Path.Combine(filesDirectory, "1.png")),
                new CapivaraTradutorUploadService.UploadData(
                    "https://example.com/upload2",
                    "key2",
                    Path.Combine(filesDirectory, "2.png"))
            };

            var result = await service.UploadFilesAsync(
                fixture.Freeze<HttpClient>(),
                uploadData,
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldUploadAllFiles()
        {
            var uploadData = new[]
            {
                new CapivaraTradutorUploadService.UploadData(
                    "https://example.com/upload1",
                    "key1",
                    Path.Combine(filesDirectory, "1.png")),
                new CapivaraTradutorUploadService.UploadData(
                    "https://example.com/upload2",
                    "key2",
                    Path.Combine(filesDirectory, "2.png"))
            };

            await service.UploadFilesAsync(
                fixture.Freeze<HttpClient>(),
                uploadData,
                cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.That.Matches(x =>
                        x.Method.Method == "PUT" &&
                        x.RequestUri!.ToString() == "https://example.com/upload1"),
                    A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.That.Matches(x =>
                        x.Method.Method == "PUT" &&
                        x.RequestUri!.ToString() == "https://example.com/upload2"),
                    A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToUploadFileShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            using var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(ERROR_MESSAGE)
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(errorResponse);

            var uploadData = new[]
            {
                new UploadData(
                    "https://example.com/upload1",
                    "key1",
                    Path.Combine(filesDirectory, "1.png"))
            };

            var result = await service.UploadFilesAsync(
                fixture.Freeze<HttpClient>(),
                uploadData,
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }

    public class CommitAsync : CapivaraTradutorUploadServiceTests
    {
        private readonly HttpResponseMessage httpResponse;
        private readonly Work work;
        private readonly Info info;

        public CommitAsync()
        {
            work = fixture.Create<Work>();
            info = CreateInfo();

            httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(@"{
                    ""status"": true,
                    ""data"": {
                        ""url"": ""sample-url""
                    }   
                }")
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(httpResponse);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResultWithChapterUrl()
        {
            var result = await service.CommitAsync(
                fixture.Freeze<HttpClient>(),
                CreateUploadData(),
                info,
                work,
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue("sample-url");
        }

        [Fact]
        public async Task GivenInvalidDecimalChapterNumberShouldSanitizeValue()
        {
            var info = CreateInfo() with { ChapterNumber = "12.1A" };

            var result = await service.CommitAsync(
                fixture.Freeze<HttpClient>(),
                CreateUploadData(),
                info,
                work,
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue("sample-url");

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.That.Matches(x => 
                        x.Content != null &&
                        x.Content.ReadAsStringAsync().GetAwaiter().GetResult().Contains(@"12.1,")),
                    A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenStatusCodeErrorShouldReturnFailResult()
        {
            using var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("anything")
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(errorResponse);

            var result = await service.CommitAsync(
                fixture.Freeze<HttpClient>(),
                CreateUploadData(),
                CreateInfo(),
                fixture.Create<Work>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("anything");
        }

        private static Info CreateInfo() => 
            new(googleDriveUrl: "https://drive.google.com/file/d/test",
                chapterName: "Test",
                chapterNumber: "1",
                chapterVolume: "1",
                message: "Message",
                titleId: 517);

        private static UploadData[] CreateUploadData() => 
        [
            new(UploadUrl: "https://example.com/upload1",
                Key: "key1",
                Path: "path1")
        ];
    }
}
