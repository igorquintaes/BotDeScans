using BotDeScans.App.Services.Logging;
using Remora.Results;
using Serilog;

namespace BotDeScans.UnitTests.Specs.Services.Logging;

public class LogEventTests : UnitTest
{
    private readonly LogEvent logEvent;

    public LogEventTests()
    {
        fixture.FreezeFake<ILogger>();

        logEvent = fixture.Create<LogEvent>();
    }

    public class AfterExecutionAsync : LogEventTests
    {
        [Fact]
        public async Task GivenSuccessCommandResultShoultNotCallErrorLog()
        {
            var commandResult = A.Fake<IResult>();
            A.CallTo(() => commandResult.IsSuccess).Returns(true);

            var result = await logEvent.AfterExecutionAsync(default!, commandResult, cancellationToken);
            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<ILogger>()
                .Error(A<string>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorCommandResultShoultCallErrorLogRecursivelyForInnerErrors()
        {
            var commandResult = A.Fake<IResult>();
            var commandResultError = A.Fake<IResultError>();
            var commandResultInner = A.Fake<IResult>();
            var commandResultInnerError = A.Fake<IResultError>();
            var commandResultInnerInner = A.Fake<IResult>();
            var commandResultInnerInnerError = A.Fake<IResultError>();

            A.CallTo(() => commandResult.IsSuccess).Returns(false);
            A.CallTo(() => commandResult.Inner).Returns(commandResultInner);
            A.CallTo(() => commandResult.Error).Returns(commandResultError);
            A.CallTo(() => commandResultError.Message).Returns("message-1");

            A.CallTo(() => commandResultInner.IsSuccess).Returns(false);
            A.CallTo(() => commandResultInner.Inner).Returns(commandResultInnerInner);
            A.CallTo(() => commandResultInner.Error).Returns(commandResultInnerError);
            A.CallTo(() => commandResultInnerError.Message).Returns("message-2");

            A.CallTo(() => commandResultInnerInner.IsSuccess).Returns(false);
            A.CallTo(() => commandResultInnerInner.Inner).Returns(null);
            A.CallTo(() => commandResultInnerInner.Error).Returns(commandResultInnerInnerError);
            A.CallTo(() => commandResultInnerInnerError.Message).Returns("message-3");

            var result = await logEvent.AfterExecutionAsync(default!, commandResult, cancellationToken);
            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<ILogger>()
                .Error(A<string>.Ignored))
                .MustHaveHappened(3, Times.Exactly);
        }
    }
}
