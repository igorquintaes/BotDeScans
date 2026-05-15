using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Logging;
using FluentResults;
using Serilog.Events;
using System.Linq.Expressions;
using ILogger = Serilog.ILogger;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class ObjectExtensionsTests : UnitTest
{
    public class SafeCallAsync : ObjectExtensionsTests
    {
        private readonly TestObject testObject;

        public SafeCallAsync() =>
            testObject = fixture.Create<TestObject>();

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            Expression<Func<TestObject, Task<Result>>> expression = obj => TestObject.SuccessMethodAsync();

            var result = await testObject.SafeCallAsync(expression);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResultWithSuccessMessage()
        {
            var successMessage = fixture.Create<string>();
            testObject.SuccessMessage = successMessage;

            Expression<Func<TestObject, Task<Result>>> expression = obj => obj.SuccessWithValueMethodAsync();

            var result = await testObject.SafeCallAsync(expression);

            result.Should().BeSuccess().And.HaveReason(successMessage);
        }

        [Fact]
        public async Task GivenFailureResultShouldReturnFailureResult()
        {
            const string ERROR_MESSAGE = "Expected error message";

            Expression<Func<TestObject, Task<Result>>> expression = obj => TestObject.FailureMethodAsync(ERROR_MESSAGE);

            var result = await testObject.SafeCallAsync(expression);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExceptionShouldReturnFailureResultWithExpectedError()
        {
            const string ERROR_MESSAGE = "Fatal error occurred. More information inside log file.";

            Expression<Func<TestObject, Task<Result>>> expression = obj => TestObject.ThrowExceptionMethodAsync();

            var result = await testObject.SafeCallAsync(expression);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExceptionShouldHaveExceptionAsCause()
        {
            const string ERROR_MESSAGE = "Fatal error occurred. More information inside log file.";
            const string EXCEPTION_MESSAGE = "Test exception";

            Expression<Func<TestObject, Task<Result>>> expression = obj => TestObject.ThrowExceptionMethodAsync();

            var result = await testObject.SafeCallAsync(expression);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
            result.Errors.Should().ContainSingle()
                  .Which.Reasons.Should().ContainSingle()
                  .Which.Should().BeOfType<ExceptionalError>()
                  .Which.Exception.Message.Should().Be(EXCEPTION_MESSAGE);
        }

        private class TestObject
        {
            public string? SuccessMessage { get; set; }

            public static Task<Result> SuccessMethodAsync() =>
                Task.FromResult(Result.Ok());

            public Task<Result> SuccessWithValueMethodAsync() =>
                Task.FromResult(Result.Ok().WithSuccess(SuccessMessage!));

            public static Task<Result> FailureMethodAsync(string errorMessage) =>
                Task.FromResult(Result.Fail(errorMessage));

            public static Task<Result> ThrowExceptionMethodAsync() =>
                throw new InvalidOperationException("Test exception");
        }
    }

    public sealed class SafeCallAsyncWithLogging : ObjectExtensionsTests, IDisposable
    {
        private readonly ILogger fakeLogger;

        public SafeCallAsyncWithLogging()
        {
            fakeLogger = A.Fake<ILogger>();
            A.CallTo(() => fakeLogger.IsEnabled(A<LogEventLevel>.Ignored)).Returns(true);

            Result.Setup(cfg => cfg.Logger = new ResultLogger(fakeLogger));
        }

        public void Dispose() =>
            Result.Setup(cfg => { });

        [Fact]
        public async Task GivenExceptionWhenLogIfFailedIsCalledShouldLogExceptionDetailsThroughResultLogger()
        {
            var testObject = fixture.Create<TestObject>();
            Expression<Func<TestObject, Task<Result>>> expression = obj => TestObject.ThrowExceptionMethodAsync();

            var result = await testObject.SafeCallAsync(expression);
            result.LogIfFailed();

            A.CallTo(fakeLogger)
                .Where(call => call.Method.Name == nameof(ILogger.Write)
                    && call.Arguments.Count >= 1
                    && Equals(call.Arguments[0], LogEventLevel.Error))
                .MustHaveHappenedOnceExactly();
        }

        private class TestObject
        {
            public static Task<Result> ThrowExceptionMethodAsync() =>
                throw new InvalidOperationException("Test exception");
        }
    }
}