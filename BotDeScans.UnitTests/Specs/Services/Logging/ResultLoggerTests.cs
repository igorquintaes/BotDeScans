using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Logging;
using FluentResults;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace BotDeScans.UnitTests.Specs.Services.Logging;

public class ResultLoggerTests : UnitTest
{
    private readonly ResultLogger resultLogger;

    public ResultLoggerTests()
    {
        fixture.FreezeFake<ILogger>();
        resultLogger = new ResultLogger(fixture.FreezeFake<ILogger>());
    }

    [Fact]
    public void GivenErrorLogLevelShouldWriteErrorToSerilog()
    {
        var result = Result.Fail("some error");

        resultLogger.Log("TestContext", "test content", result, LogLevel.Error);

        A.CallTo(fixture.FreezeFake<ILogger>())
            .Where(call => call.Method.Name == nameof(ILogger.Write)
                && call.Arguments.Count >= 1
                && Equals(call.Arguments[0], LogEventLevel.Error))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GivenInfoLogLevelShouldWriteInformationToSerilog()
    {
        var result = Result.Ok();

        resultLogger.Log("TestContext", "test content", result, LogLevel.Information);

        A.CallTo(fixture.FreezeFake<ILogger>())
            .Where(call => call.Method.Name == nameof(ILogger.Write)
                && call.Arguments.Count >= 1
                && Equals(call.Arguments[0], LogEventLevel.Information))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GivenWarningLogLevelShouldWriteWarningToSerilog()
    {
        var result = Result.Ok();

        resultLogger.Log("TestContext", "test content", result, LogLevel.Warning);

        A.CallTo(fixture.FreezeFake<ILogger>())
            .Where(call => call.Method.Name == nameof(ILogger.Write)
                && call.Arguments.Count >= 1
                && Equals(call.Arguments[0], LogEventLevel.Warning))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GivenGenericLogShouldUseTypeNameAsContext()
    {
        var result = Result.Fail("error");

        resultLogger.Log<ResultLoggerTests>("test content", result, LogLevel.Error);

        A.CallTo(fixture.FreezeFake<ILogger>())
            .Where(call => call.Method.Name == nameof(ILogger.Write)
                && call.Arguments.Count >= 1
                && Equals(call.Arguments[0], LogEventLevel.Error))
            .MustHaveHappenedOnceExactly();
    }
}
