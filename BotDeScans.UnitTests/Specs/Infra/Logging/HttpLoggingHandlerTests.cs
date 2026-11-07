using BotDeScans.App.Infra.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using System.Net;

namespace BotDeScans.UnitTests.Specs.Infra.Logging;

public class HttpLoggingHandlerTests : UnitTest, IDisposable
{
    private readonly HttpMessageHandler innerHandler;
    private readonly HttpClient httpClient;

    public HttpLoggingHandlerTests()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();

        innerHandler = A.Fake<HttpMessageHandler>();
        httpClient = new HttpClient(new HttpLoggingHandler(innerHandler));
    }

    public void Dispose()
    {
        ClearInMemorySink();
        Log.CloseAndFlush();
        httpClient.Dispose();

        GC.SuppressFinalize(this);
    }

    private static void ClearInMemorySink() =>
        InMemorySink.Instance.Dispose();

    public class SendAsync : HttpLoggingHandlerTests
    {
        private readonly HttpResponseMessage response;

        public SendAsync()
        {
            response = new HttpResponseMessage(HttpStatusCode.OK);

            A.CallTo(innerHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .WithReturnType<Task<HttpResponseMessage>>()
                .Returns(Task.FromResult(response));
        }

        [Fact]
        public async Task GivenRequestShouldCallInnerHandler()
        {
            await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            A.CallTo(innerHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenRequestShouldReturnResponseFromInnerHandler()
        {
            var result = await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            result.Should().Be(response);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GivenAnyStatusCodeShouldReturnResponseFromInnerHandler(HttpStatusCode statusCode)
        {
            response.StatusCode = statusCode;

            var result = await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            result.StatusCode.Should().Be(statusCode);
        }

        [Fact]
        public async Task GivenPostMethodShouldDelegateToInnerHandler()
        {
            await httpClient.PostAsync(
                "https://example.com/test",
                null,
                cancellationToken);

            A.CallTo(innerHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenDeleteMethodShouldDelegateToInnerHandler()
        {
            await httpClient.DeleteAsync(
                "https://example.com/test",
                cancellationToken);

            A.CallTo(innerHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenPutMethodShouldDelegateToInnerHandler()
        {
            await httpClient.PutAsync(
                "https://example.com/test",
                null,
                cancellationToken);

            A.CallTo(innerHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenCancellationTokenShouldPassToInnerHandler()
        {
            await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            A.CallTo(innerHandler)
                .Where(call => call.Method.Name == "SendAsync")
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessRequestShouldLogInformation()
        {
            ClearInMemorySink();

            await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            InMemorySink.Instance.LogEvents
                .Should().HaveCount(2)
                .And.AllSatisfy(logEvent => logEvent.Level.Should().Be(LogEventLevel.Information));
        }

        [Fact]
        public async Task GivenSuccessRequestShouldLogStartMessage()
        {
            ClearInMemorySink();

            await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            var startLog = InMemorySink.Instance.LogEvents.First();
            startLog.MessageTemplate.Text.Should().Contain("iniciado");
            startLog.Properties["Method"].ToString().Should().Contain("GET");
            startLog.Properties["Url"].ToString().Should().Contain("https://example.com/test");
        }

        [Fact]
        public async Task GivenSuccessRequestShouldLogSuccessMessage()
        {
            ClearInMemorySink();

            await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            var successLog = InMemorySink.Instance.LogEvents.Last();
            successLog.MessageTemplate.Text.Should().Contain("sucesso");
            successLog.Properties["Method"].ToString().Should().Contain("GET");
            successLog.Properties["StatusCode"].ToString().Should().Contain("200");
            successLog.Properties["Url"].ToString().Should().Contain("https://example.com/test");
        }

        [Fact]
        public async Task GivenFailureRequestShouldLogWarning()
        {
            ClearInMemorySink();
            response.StatusCode = HttpStatusCode.NotFound;

            await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            var logEvents = InMemorySink.Instance.LogEvents.ToList();
            logEvents.Should().HaveCount(2);
            logEvents[0].Level.Should().Be(LogEventLevel.Information);
            logEvents[1].Level.Should().Be(LogEventLevel.Warning);
        }

        [Fact]
        public async Task GivenFailureRequestShouldLogFailureMessage()
        {
            ClearInMemorySink();
            response.StatusCode = HttpStatusCode.NotFound;

            await httpClient.GetAsync(
                "https://example.com/test",
                cancellationToken);

            var failureLog = InMemorySink.Instance.LogEvents.Last();
            failureLog.MessageTemplate.Text.Should().Contain("falha");
            failureLog.Properties["Method"].ToString().Should().Contain("GET");
            failureLog.Properties["StatusCode"].ToString().Should().Contain("404");
            failureLog.Properties["Url"].ToString().Should().Contain("https://example.com/test");
        }
    }
}
