using BotDeScans.App.Extensions;
using MangaDexSharp;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class MangaDexResponseExtensionsTests : UnitTest
{
    public class AsResult : MangaDexResponseExtensionsTests
    {
        [Fact]
        public void GivenResponseWithoutErrorsShouldReturnSuccessResult()
        {
            var mangaDexResponse = new MangaDexRoot
            {
                Result = "ok",
                Errors = []
            };

            var result = mangaDexResponse.AsResult();

            result.Should().BeSuccess();
        }

        [Fact]
        public void GivenResponseWithSingleErrorShouldReturnFailureResult()
        {
            const int STATUS_CODE = 400;
            const string TITLE = "Bad Request";
            const string DETAIL = "Invalid parameters";

            var mangaDexResponse = new MangaDexRoot
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError
                    {
                        Status = STATUS_CODE,
                        Title = TITLE,
                        Detail = DETAIL
                    }
                ]
            };

            var result = mangaDexResponse.AsResult();

            result.Should().BeFailure();
            result.Errors.Should().ContainSingle()
                  .Which.Message.Should().Be($"{STATUS_CODE} - {TITLE} - {DETAIL}");
        }

        [Fact]
        public void GivenResponseWithMultipleErrorsShouldReturnAllErrors()
        {
            var mangaDexResponse = new MangaDexRoot
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError { Status = 400, Title = "Error 1", Detail = "Detail 1" },
                    new MangaDexError { Status = 401, Title = "Error 2", Detail = "Detail 2" },
                    new MangaDexError { Status = 403, Title = "Error 3", Detail = "Detail 3" }
                ]
            };

            var result = mangaDexResponse.AsResult();

            result.Should().BeFailure();
            result.Errors.Should().HaveCount(3);
            result.Errors.Select(e => e.Message).Should().BeEquivalentTo(
            [
                "400 - Error 1 - Detail 1",
                "401 - Error 2 - Detail 2",
                "403 - Error 3 - Detail 3"
            ]);
        }

        [Fact]
        public void GivenAllowedStatusCodesShouldFilterOutMatchingErrors()
        {
            const int ALLOWED_STATUS = 404;
            const int ERROR_STATUS = 400;

            var mangaDexResponse = new MangaDexRoot
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError { Status = ALLOWED_STATUS, Title = "Not Found", Detail = "Resource not found" },
                    new MangaDexError { Status = ERROR_STATUS, Title = "Bad Request", Detail = "Invalid request" }
                ]
            };

            var result = mangaDexResponse.AsResult(ALLOWED_STATUS);

            result.Should().BeFailure();
            result.Errors.Should().ContainSingle()
                  .Which.Message.Should().Contain($"{ERROR_STATUS}");
        }

        [Fact]
        public void GivenMultipleAllowedStatusCodesShouldFilterOutAllMatching()
        {
            const int ALLOWED_STATUS_1 = 404;
            const int ALLOWED_STATUS_2 = 409;
            const int ERROR_STATUS = 400;

            var mangaDexResponse = new MangaDexRoot
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError { Status = ALLOWED_STATUS_1, Title = "Not Found", Detail = "Detail 1" },
                    new MangaDexError { Status = ERROR_STATUS, Title = "Bad Request", Detail = "Detail 2" },
                    new MangaDexError { Status = ALLOWED_STATUS_2, Title = "Conflict", Detail = "Detail 3" }
                ]
            };

            var result = mangaDexResponse.AsResult(ALLOWED_STATUS_1, ALLOWED_STATUS_2);

            result.Should().BeFailure();
            result.Errors.Should().ContainSingle()
                  .Which.Message.Should().Contain($"{ERROR_STATUS}");
        }

        [Fact]
        public void GivenAllErrorsAreAllowedShouldReturnSuccessResult()
        {
            const int ALLOWED_STATUS_1 = 404;
            const int ALLOWED_STATUS_2 = 409;

            var mangaDexResponse = new MangaDexRoot
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError { Status = ALLOWED_STATUS_1, Title = "Not Found", Detail = "Detail 1" },
                    new MangaDexError { Status = ALLOWED_STATUS_2, Title = "Conflict", Detail = "Detail 2" }
                ]
            };

            var result = mangaDexResponse.AsResult(ALLOWED_STATUS_1, ALLOWED_STATUS_2);

            result.Should().BeSuccess();
        }
    }

    public class AsResultGeneric : MangaDexResponseExtensionsTests
    {
        [Fact]
        public void GivenResponseWithoutErrorsShouldReturnSuccessResultWithData()
        {
            var expectedData = fixture.Create<TestData>();
            var mangaDexResponse = new MangaDexRoot<TestData>
            {
                Result = "ok",
                Data = expectedData,
                Errors = []
            };

            var result = mangaDexResponse.AsResult();

            result.Should().BeSuccess().And.HaveValue(expectedData);
        }

        [Fact]
        public void GivenResponseWithSingleErrorShouldReturnFailureResult()
        {
            const int STATUS_CODE = 400;
            const string TITLE = "Bad Request";
            const string DETAIL = "Invalid parameters";

            var mangaDexResponse = new MangaDexRoot<TestData>
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError
                    {
                        Status = STATUS_CODE,
                        Title = TITLE,
                        Detail = DETAIL
                    }
                ]
            };

            var result = mangaDexResponse.AsResult();

            result.Should().BeFailure();
            result.Errors.Should().ContainSingle()
                  .Which.Message.Should().Be($"{STATUS_CODE} - {TITLE} - {DETAIL}");
        }

        [Fact]
        public void GivenResponseWithMultipleErrorsShouldReturnAllErrors()
        {
            var mangaDexResponse = new MangaDexRoot<TestData>
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError { Status = 400, Title = "Error 1", Detail = "Detail 1" },
                    new MangaDexError { Status = 401, Title = "Error 2", Detail = "Detail 2" }
                ]
            };

            var result = mangaDexResponse.AsResult();

            result.Should().BeFailure();
            result.Errors.Should().HaveCount(2);
        }

        [Fact]
        public void GivenAllowedStatusCodesShouldFilterOutMatchingErrors()
        {
            const int ALLOWED_STATUS = 404;
            const int ERROR_STATUS = 400;

            var mangaDexResponse = new MangaDexRoot<TestData>
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError { Status = ALLOWED_STATUS, Title = "Not Found", Detail = "Resource not found" },
                    new MangaDexError { Status = ERROR_STATUS, Title = "Bad Request", Detail = "Invalid request" }
                ]
            };

            var result = mangaDexResponse.AsResult(ALLOWED_STATUS);

            result.Should().BeFailure();
            result.Errors.Should().ContainSingle()
                  .Which.Message.Should().Contain($"{ERROR_STATUS}");
        }

        [Fact]
        public void GivenMultipleAllowedStatusCodesShouldFilterOutAllMatchingAndReturnData()
        {
            const int ALLOWED_STATUS_1 = 404;
            const int ALLOWED_STATUS_2 = 409;
            const int ERROR_STATUS = 400;

            var mangaDexResponse = new MangaDexRoot<TestData>
            {
                Result = "error",
                Errors =
                [
                    new MangaDexError { Status = ALLOWED_STATUS_1, Title = "Not Found", Detail = "Detail 1" },
                    new MangaDexError { Status = ERROR_STATUS, Title = "Bad Request", Detail = "Detail 2" },
                    new MangaDexError { Status = ALLOWED_STATUS_2, Title = "Conflict", Detail = "Detail 3" }
                ]
            };

            var result = mangaDexResponse.AsResult(ALLOWED_STATUS_1, ALLOWED_STATUS_2);

            result.Should().BeFailure();
            result.Errors.Should().ContainSingle()
                  .Which.Message.Should().Contain($"{ERROR_STATUS}");
        }

        [Fact]
        public void GivenAllErrorsAreAllowedShouldReturnSuccessResultWithData()
        {
            const int ALLOWED_STATUS_1 = 404;
            const int ALLOWED_STATUS_2 = 409;
            var expectedData = fixture.Create<TestData>();

            var mangaDexResponse = new MangaDexRoot<TestData>
            {
                Result = "error",
                Data = expectedData,
                Errors =
                [
                    new MangaDexError { Status = ALLOWED_STATUS_1, Title = "Not Found", Detail = "Detail 1" },
                    new MangaDexError { Status = ALLOWED_STATUS_2, Title = "Conflict", Detail = "Detail 2" }
                ]
            };

            var result = mangaDexResponse.AsResult(ALLOWED_STATUS_1, ALLOWED_STATUS_2);

            result.Should().BeSuccess();
            result.Value.Should().Be(expectedData);
        }

        [Fact]
        public void GivenNullDataShouldReturnResultWithNullValue()
        {
            var mangaDexResponse = new MangaDexRoot<TestData?>
            {
                Result = "ok",
                Data = null,
                Errors = []
            };

            var result = mangaDexResponse.AsResult();

            result.Should().BeSuccess();
            result.Value.Should().BeNull();
        }

        private class TestData
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}