using BotDeScans.App.Services;
using BotDeScans.UnitTests.FakeObjects;
using System.Net;

namespace BotDeScans.UnitTests.Specs.Services;

// todo: seria bom futuramente trocarmos os mocks de client por um wiremock
public class SakuraMangasServiceTests : UnitTest
{
    private readonly SakuraMangasService service;

    public SakuraMangasServiceTests()
    {
        fixture.Inject(new HttpClient(fixture.FreezeFake<FakeHttpMessageHandler>()));

        service = fixture.Create<SakuraMangasService>();
    }

    public class UploadAsync : SakuraMangasServiceTests, IDisposable
    {
        private readonly HttpResponseMessage httpResponse;
        private readonly string uploadFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "fake-file.zip");

        public UploadAsync()
        {
            if (File.Exists(uploadFilePath) is false)
                File.Create(uploadFilePath).Dispose();

            httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(@"{ ""message"": ""Upload realizado com sucesso."", ""id_capitulo"": 185550, ""chapter_url"": ""https://sakuramangas.org/obras/tail-star/111"" }")
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
            var chapterNumber = fixture.Create<string>();
            var chapterName = fixture.Create<string>();
            var mangaDexId = fixture.Create<string>();

            var result = await service.UploadAsync(
                chapterNumber, 
                chapterName, 
                mangaDexId,
                uploadFilePath, 
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue("https://sakuramangas.org/obras/tail-star/111");
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldPostExpectedData()
        {
            var chapterNumber = fixture.Create<string>();
            var chapterName = fixture.Create<string>();
            var mangaDexId = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.That.Matches(x =>
                        x.Method.Method == "POST" &&
                        x.Content is MultipartFormDataContent),
                    A<CancellationToken>.Ignored))
                .Invokes((HttpRequestMessage request, CancellationToken _) =>
                    Verify(request.Content.As<MultipartFormDataContent>()).GetAwaiter().GetResult())
                .Returns(httpResponse);

            await service.UploadAsync(
                chapterNumber,
                chapterName,
                mangaDexId,
                uploadFilePath,
                cancellationToken);
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

            var result = await service.UploadAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<string>(),
                uploadFilePath,
                cancellationToken);

            result.Should()
                .BeFailure().And
                .HaveError("Erro ao se comunicar com a Sakura Mangás. Cheque o arquivo de logs para mais detalhes.")
                .Which.Errors.Single().Reasons.Single().Message.Should().Be("anything");
        }

        [Fact]
        public async Task GivenBodyErrorShouldReturnFailResult()
        {
            using var errorResponse = new HttpResponseMessage()
            {
                Content = new StringContent(@"{""message"": ""E-mail ou senha incorretos."", ""error_code"": ""AUTH_INVALID"" }")
            };

            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.Ignored,
                    A<CancellationToken>.Ignored))
                .Returns(errorResponse);

            var result = await service.UploadAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                fixture.Create<string>(),
                uploadFilePath,
                cancellationToken);

            result.Should()
                .BeFailure().And
                .HaveError("E-mail ou senha incorretos.")
                .Which.Errors.Single().Reasons.Single().Message.Should().Be(@"{""message"": ""E-mail ou senha incorretos."", ""error_code"": ""AUTH_INVALID"" }");
        }

        public void Dispose()
        {
            httpResponse.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    public class PingCredentialsAsync : SakuraMangasServiceTests, IDisposable
    {
        private readonly HttpResponseMessage httpResponse;

        public PingCredentialsAsync()
        {
            httpResponse = new HttpResponseMessage
            {
                Content = new StringContent(@"{""message"": ""Manga não encontrado para este mangadexid."", ""error_code"": ""MANGA_NOT_FOUND""}")
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
            var result = await service.PingCredentialsAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldPostExpectedData()
        {
            A.CallTo(() => fixture
                .FreezeFake<FakeHttpMessageHandler>()
                .FakeSendAsync(
                    A<HttpRequestMessage>.That.Matches(x =>
                        x.Method.Method == "POST" &&
                        x.Content is MultipartFormDataContent),
                    A<CancellationToken>.Ignored))
                .Invokes((HttpRequestMessage request, CancellationToken _) =>
                    Verify(request.Content.As<MultipartFormDataContent>()).GetAwaiter().GetResult())
                .Returns(httpResponse);

            await service.PingCredentialsAsync(cancellationToken);
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

            var result = await service.PingCredentialsAsync(cancellationToken);

            result.Should()
                .BeFailure().And
                .HaveError("Erro ao se comunicar com a Sakura Mangás. Cheque o arquivo de logs para mais detalhes.")
                .Which.Errors.Single().Reasons.Single().Message.Should().Be("anything");
        }

        [Fact]
        public async Task GivenBodyErrorShouldReturnFailResult()
        {
            using var errorResponse = new HttpResponseMessage()
            {
                Content = new StringContent(@"{""message"": ""E-mail ou senha incorretos."", ""error_code"": ""AUTH_INVALID"" }")
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
                .HaveError("E-mail ou senha incorretos.")
                .Which.Errors.Single().Reasons.Single().Message.Should().Be(@"{""message"": ""E-mail ou senha incorretos."", ""error_code"": ""AUTH_INVALID"" }");
        }

        public void Dispose()
        {
            httpResponse.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
