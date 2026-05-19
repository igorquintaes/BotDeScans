using BotDeScans.App.Extensions;
using FluentResults;
using Google;
using System.Net;
using System.Text.Json;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class FluentResultsExtensionsTests : UnitTest
{
    private const string VALUE = "Value.";
    private const string SUCCESS_MESSAGE = "Success.";
    private const string ERROR_MESSAGE = "Error.";
    private const string EXCEPTION_MESSAGE = "Exception.";

    public class ToValidationErrorMessage : FluentResultsExtensionsTests
    {
        [Fact]
        public void GivenSuccessResultShouldReturnSuccessMessage()
        {
            const string EXPECTED_MESSAGE = "Success.";

            var result = Result.Ok();

            var message = result.ToValidationErrorMessage();

            message.Should().Be(EXPECTED_MESSAGE);
        }

        [Fact]
        public void GivenSingleErrorShouldReturnErrorMessage()
        {
            const string ERROR_MESSAGE = "Test error message";

            var result = Result.Fail(ERROR_MESSAGE);

            var message = result.ToValidationErrorMessage();

            message.Should().Be(ERROR_MESSAGE);
        }

        [Fact]
        public void GivenMultipleErrorsShouldReturnJoinedErrorMessages()
        {
            const string ERROR_1 = "First error";
            const string ERROR_2 = "Second error";
            const string ERROR_3 = "Third error";
            const string EXPECTED_MESSAGE = "First error; Second error; Third error";

            var result = Result.Fail([ERROR_1, ERROR_2, ERROR_3]);

            var message = result.ToValidationErrorMessage();

            message.Should().Be(EXPECTED_MESSAGE);
        }

        [Fact]
        public void GivenErrorsWithNestedReasonsShouldIncludeAllMessages()
        {
            const string ERROR_MESSAGE = "Main error";
            const string NESTED_ERROR = "Nested error";
            var expectedMessage = $"{ERROR_MESSAGE}; {NESTED_ERROR}";

            var nestedError = new Error(NESTED_ERROR);
            var mainError = new Error(ERROR_MESSAGE).CausedBy(nestedError);
            var result = Result.Fail(mainError);

            var message = result.ToValidationErrorMessage();

            message.Should().Be(expectedMessage);
        }
    }

    public class ToDiscordResult : FluentResultsExtensionsTests
    {
        [Fact]
        public void GivenSuccessResultShouldReturnSuccessDiscordResult()
        {
            var result = Result.Ok();

            var discordResult = result.ToDiscordResult();

            discordResult.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void GivenFailureResultShouldReturnFailureDiscordResult()
        {
            const string ERROR_MESSAGE = "Test error";

            var result = Result.Fail(ERROR_MESSAGE);

            var discordResult = result.ToDiscordResult();

            discordResult.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void GivenFailureResultShouldReturnDiscordResultWithSerializedErrors()
        {
            const string ERROR_MESSAGE = "Test error";

            var result = Result.Fail(ERROR_MESSAGE);
            var discordResult = result.ToDiscordResult();

            var expectedErrorsInfo = new[]
            {
                new ErrorInfo(ERROR_MESSAGE, 1, 0, ErrorType.Regular)
            };
            var expectedJson = JsonSerializer.Serialize(expectedErrorsInfo);

            discordResult.IsSuccess.Should().BeFalse();
            discordResult.Error.Should().NotBeNull();
            discordResult.Error!.Message.Should().Be(expectedJson);
        }
    }

    public class GetErrorsInfo : FluentResultsExtensionsTests
    {
        [Fact]
        public void GivenRegularErrorShouldReturnExpectedErrorInfo()
        {
            var errors = new List<IError> { new Error(ERROR_MESSAGE) };

            var expectedErrorInfo = new ErrorInfo(
                Message: ERROR_MESSAGE,
                Number: 1,
                Depth: 0,
                Type: ErrorType.Regular);

            errors.GetErrorsInfo()
                  .Should().ContainSingle()
                  .Which.Should().BeEquivalentTo(expectedErrorInfo);
        }

        [Fact]
        public void GivenMultipleErrorsShouldReturnCorrectNumbering()
        {
            const string ERROR_1 = "First error";
            const string ERROR_2 = "Second error";
            const string ERROR_3 = "Third error";

            var errors = new List<IError>
            {
                new Error(ERROR_1),
                new Error(ERROR_2),
                new Error(ERROR_3)
            };

            var errorsInfo = errors.GetErrorsInfo().ToList();

            errorsInfo.Should().HaveCount(3);
            errorsInfo[0].Number.Should().Be(1);
            errorsInfo[1].Number.Should().Be(2);
            errorsInfo[2].Number.Should().Be(3);
        }

        [Fact]
        public void GivenNestedErrorsShouldReturnCorrectDepth()
        {
            const string MAIN_ERROR = "Main error";
            const string NESTED_ERROR_1 = "Nested error 1";
            const string NESTED_ERROR_2 = "Nested error 2";

            var nestedError2 = new Error(NESTED_ERROR_2);
            var nestedError1 = new Error(NESTED_ERROR_1).CausedBy(nestedError2);
            var mainError = new Error(MAIN_ERROR).CausedBy(nestedError1);
            var errors = new List<IError> { mainError };

            var errorsInfo = errors.GetErrorsInfo().ToList();

            errorsInfo.Should().HaveCount(3);
            errorsInfo[0].Depth.Should().Be(0);
            errorsInfo[0].Message.Should().Be(MAIN_ERROR);
            errorsInfo[1].Depth.Should().Be(1);
            errorsInfo[1].Message.Should().Be(NESTED_ERROR_1);
            errorsInfo[2].Depth.Should().Be(2);
            errorsInfo[2].Message.Should().Be(NESTED_ERROR_2);
        }

        [Fact]
        public void GivenExceptionalErrorShouldReturnExceptionMessage()
        {
            var exception = new InvalidOperationException(EXCEPTION_MESSAGE);
            var errors = new List<IError> { new ExceptionalError(exception) };

            var expectedErrorInfo = new ErrorInfo(
                Message: EXCEPTION_MESSAGE,
                Number: 1,
                Depth: 0,
                Type: ErrorType.Exception);

            errors.GetErrorsInfo()
                  .Should().ContainSingle()
                  .Which.Should().BeEquivalentTo(expectedErrorInfo);
        }

        [Fact]
        public void GivenGoogleApiExceptionShouldReturnFormattedMessage()
        {
            var statusCode = HttpStatusCode.NotFound;
            var googleException = new GoogleApiException(
                serviceName: "Google", 
                message: "Test Google error")
            {
                HttpStatusCode = statusCode
            };

            var errors = new List<IError> { new ExceptionalError(googleException) };

            var expectedErrorInfo = new ErrorInfo(
                Message: string.Empty,
                Number: 1,
                Depth: 0,
                Type: ErrorType.Exception);

            errors.GetErrorsInfo()
                  .Should().ContainSingle()
                  .Which.Should().BeEquivalentTo(expectedErrorInfo,
                      options => options.Excluding(x => x.Message))
                  .And.Match<ErrorInfo>(x =>
                       x.Message.Contains(nameof(HttpStatusCode.NotFound)) && 
                       x.Message.Contains(HttpStatusCode.NotFound.ToString()));
        }

        [Fact]
        public void GivenMixedErrorTypesShouldReturnCorrectTypes()
        {
            var errors = new List<IError>
            {
                new Error(ERROR_MESSAGE),
                new ExceptionalError(new InvalidOperationException(EXCEPTION_MESSAGE))
            };

            errors.GetErrorsInfo()
                  .Should().HaveCount(2)
                  .And.HaveElementAt(0, new(ERROR_MESSAGE, 1, 0, ErrorType.Regular))
                  .And.HaveElementAt(1, new(EXCEPTION_MESSAGE, 2, 0, ErrorType.Exception));
        }
    }

    public class Map : FluentResultsExtensionsTests
    {
        [Fact]
        public void GivenSuccessResultShouldMapToNewValue() => 
            Result.Ok(1)
                  .Map(VALUE)
                  .Should().BeSuccess()
                  .And.HaveValue(VALUE);

        [Fact]
        public void GivenFailedResultShouldPreserveFailure() => 
            Result.Fail<int>(ERROR_MESSAGE)
                  .Map("mapped-value")
                  .Should()
                  .BeFailure()
                  .And.HaveError(ERROR_MESSAGE);

        [Fact]
        public void GivenSuccessResultWithReasonsShouldPreserveReasons() => 
            Result.Ok(1)
                  .WithSuccess(SUCCESS_MESSAGE)
                  .Map(VALUE)
                  .Should().BeSuccess()
                  .And.HaveValue(VALUE)
                  .And.HaveReason(SUCCESS_MESSAGE);
    }

    public class Set : FluentResultsExtensionsTests
    {
        [Fact]
        public void GivenSuccessResultShouldReturnOkWithFactoryValue() => 
            Result.Ok()
                  .Set(VALUE)
                  .Should().BeSuccess()
                  .And.HaveValue(VALUE);

        [Fact]
        public void GivenSuccessResultWithReasonsShouldPreserveReasons() => 
            Result.Ok()
                  .WithSuccess(SUCCESS_MESSAGE)
                  .Set(VALUE)
                  .Should().BeSuccess()
                  .And.HaveValue(VALUE)
                  .And.HaveReason(SUCCESS_MESSAGE);

        [Fact]
        public void GivenFailedResultShouldReturnFailWithErrors() => 
            Result.Fail(ERROR_MESSAGE)
                  .Set(VALUE)
                  .Should().BeFailure()
                  .And.HaveError(ERROR_MESSAGE);

        [Fact]
        public void GivenFailedResultWithSuccessesShouldPreserveBothReasons() => 
            Result.Ok()
                  .WithSuccess(SUCCESS_MESSAGE)
                  .WithError(ERROR_MESSAGE)
                  .Set(VALUE)
                  .Should().BeFailure()
                  .And.HaveReason(SUCCESS_MESSAGE)
                  .And.HaveError(ERROR_MESSAGE);
    }

    public class SafeCallAsyncWithFuncTaskT : FluentResultsExtensionsTests
    {
        [Fact]
        public async Task GivenSuccessfulFuncShouldReturnOkWithValue()
        {
            var result = await new Result().SafeCallAsync(
                func:  () => Task.FromResult(VALUE),
                error: new Error(ERROR_MESSAGE));

            result.Should().BeSuccess()
                  .And.HaveValue(VALUE);
        }

        [Fact]
        public async Task GivenThrowingFuncShouldReturnFailWithError()
        {
            var result = await new Result().SafeCallAsync<int>(
                func:  () => throw new InvalidOperationException(EXCEPTION_MESSAGE),
                error: new Error(ERROR_MESSAGE));

            result.Should().BeFailure()
                  .And.HaveError(ERROR_MESSAGE)
                  .Which.Errors.Should().ContainSingle()
                  .Which.Reasons.Should().ContainSingle()
                  .Which.Should().BeOfType<ExceptionalError>()
                  .Which.Exception.Should().BeOfType<InvalidOperationException>()
                  .Which.Message.Should().Be(EXCEPTION_MESSAGE);
        }

        [Fact]
        public async Task GivenResultWithReasonsShouldPropagateWhenTIsNotResultBase()
        {
            var result = await Result.Ok().WithSuccess(SUCCESS_MESSAGE).SafeCallAsync(
                func: () => Task.FromResult(VALUE),
                error: new Error(ERROR_MESSAGE));

            result.Should().BeSuccess()
                  .Which.Should().HaveValue(VALUE)
                  .And.HaveReason(SUCCESS_MESSAGE);
        }

        [Fact]
        public async Task GivenFailedResultWithSuccessShouldPropagateThemWithError()
        {
            var result = await Result.Ok().WithSuccess(SUCCESS_MESSAGE).SafeCallAsync<int>(
                func: () => throw new InvalidOperationException(EXCEPTION_MESSAGE),
                error: new Error(ERROR_MESSAGE));

            result.Should().BeFailure()
                  .And.HaveReason(SUCCESS_MESSAGE)
                  .And.HaveError(ERROR_MESSAGE)
                  .Which.Errors.Should().ContainSingle()
                  .Which.Reasons.Should().ContainSingle()
                  .Which.Should().BeOfType<ExceptionalError>()
                  .Which.Exception.Should().BeOfType<InvalidOperationException>()
                  .Which.Message.Should().Be(EXCEPTION_MESSAGE);
        }
    }
}