using BotDeScans.App.Extensions;
using FluentResults;
using Google;
using System.Net;
using System.Text.Json;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class FluentResultsExtensionsTests : UnitTest
{
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

            discordResult.Error.Should().NotBeNull();
            discordResult.Error!.Message.Should().Be(expectedJson);
        }
    }

    public class FailIf : FluentResultsExtensionsTests
    {
        [Fact]
        public void GivenConditionTrueShouldAddError()
        {
            const string ERROR_MESSAGE = "Condition failed";

            var result = Result.Ok().FailIf(() => true, ERROR_MESSAGE);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public void GivenConditionFalseShouldNotAddError()
        {
            const string ERROR_MESSAGE = "Condition failed";

            var result = Result.Ok().FailIf(() => false, ERROR_MESSAGE);

            result.Should().BeSuccess();
        }

        [Fact]
        public void GivenMultipleConditionsShouldAccumulateErrors()
        {
            const string ERROR_1 = "First error";
            const string ERROR_2 = "Second error";

            var result = Result.Ok()
                .FailIf(() => true, ERROR_1)
                .FailIf(() => true, ERROR_2);

            result.Should().BeFailure();
            result.Errors.Should().HaveCount(2);
            result.Errors.Select(e => e.Message).Should().BeEquivalentTo([ERROR_1, ERROR_2]);
        }

        [Fact]
        public void GivenAlreadyFailedResultShouldPreserveExistingErrors()
        {
            const string EXISTING_ERROR = "Existing error";
            const string NEW_ERROR = "New error";

            var result = Result.Fail(EXISTING_ERROR)
                .FailIf(() => true, NEW_ERROR);

            result.Should().BeFailure();
            result.Errors.Should().HaveCount(2);
            result.Errors.Select(e => e.Message).Should().BeEquivalentTo([EXISTING_ERROR, NEW_ERROR]);
        }
    }

    public class GetErrorsInfo : FluentResultsExtensionsTests
    {
        [Fact]
        public void GivenRegularErrorShouldReturnExpectedErrorInfo()
        {
            const string ERROR_MESSAGE = "Test error";

            var errors = new List<IError> { new Error(ERROR_MESSAGE) };

            var errorsInfo = errors.GetErrorsInfo().ToList();

            errorsInfo.Should().ContainSingle();
            errorsInfo[0].Message.Should().Be(ERROR_MESSAGE);
            errorsInfo[0].Number.Should().Be(1);
            errorsInfo[0].Depth.Should().Be(0);
            errorsInfo[0].Type.Should().Be(ErrorType.Regular);
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
            const string EXCEPTION_MESSAGE = "Test exception message";
            var exception = new InvalidOperationException(EXCEPTION_MESSAGE);
            var errors = new List<IError> { new ExceptionalError(exception) };

            var errorsInfo = errors.GetErrorsInfo().ToList();

            errorsInfo.Should().ContainSingle();
            errorsInfo[0].Message.Should().Be(EXCEPTION_MESSAGE);
            errorsInfo[0].Type.Should().Be(ErrorType.Exception);
        }

        [Fact]
        public void GivenGoogleApiExceptionShouldReturnFormattedMessage()
        {
            var statusCode = HttpStatusCode.NotFound;
            var googleException = new GoogleApiException("Google", "Test Google error")
            {
                HttpStatusCode = statusCode
            };
            var errors = new List<IError> { new ExceptionalError(googleException) };

            var errorsInfo = errors.GetErrorsInfo().ToList();

            errorsInfo.Should().ContainSingle();
            errorsInfo[0].Message.Should().Contain("NotFound");
            errorsInfo[0].Message.Should().Contain("404");
            errorsInfo[0].Message.Should().Contain("http.cat");
            errorsInfo[0].Type.Should().Be(ErrorType.Exception);
        }

        [Fact]
        public void GivenMixedErrorTypesShouldReturnCorrectTypes()
        {
            const string REGULAR_ERROR = "Regular error";
            const string EXCEPTION_MESSAGE = "Exception message";

            var exception = new InvalidOperationException(EXCEPTION_MESSAGE);
            var errors = new List<IError>
            {
                new Error(REGULAR_ERROR),
                new ExceptionalError(exception)
            };

            var errorsInfo = errors.GetErrorsInfo().ToList();

            errorsInfo.Should().HaveCount(2);
            errorsInfo[0].Type.Should().Be(ErrorType.Regular);
            errorsInfo[0].Message.Should().Be(REGULAR_ERROR);
            errorsInfo[1].Type.Should().Be(ErrorType.Exception);
            errorsInfo[1].Message.Should().Be(EXCEPTION_MESSAGE);
        }
    }
}