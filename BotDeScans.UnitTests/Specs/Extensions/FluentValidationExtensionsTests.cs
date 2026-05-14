using BotDeScans.App.Extensions;
using FluentValidation.Results;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class FluentValidationExtensionsTests : UnitTest
{
    public class ToResult : FluentValidationExtensionsTests
    {
        [Fact]
        public void GivenValidValidationResultShouldReturnSuccessResult()
        {
            var validationResult = new ValidationResult();

            var result = validationResult.ToResult();

            result.Should().BeSuccess();
        }

        [Fact]
        public void GivenInvalidValidationResultWithSingleErrorShouldReturnFailureResult()
        {
            const string ERROR_MESSAGE = "Validation error";
            const string PROPERTY_NAME = "PropertyName";

            var validationFailure = new ValidationFailure(PROPERTY_NAME, ERROR_MESSAGE);
            var validationResult = new ValidationResult([validationFailure]);

            var result = validationResult.ToResult();

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
            result.Errors.Should().HaveCount(1);
            result.Errors.Single().Metadata.Should()
                .ContainKey(nameof(ValidationFailure.PropertyName))
                .WhoseValue.Should().Be(PROPERTY_NAME);
        }

        [Fact]
        public void GivenInvalidValidationResultWithMultipleErrorsShouldReturnAllErrors()
        {
            const string ERROR_1 = "First validation error";
            const string ERROR_2 = "Second validation error";
            const string ERROR_3 = "Third validation error";

            var validationFailures = new[]
            {
                new ValidationFailure("Property1", ERROR_1),
                new ValidationFailure("Property2", ERROR_2),
                new ValidationFailure("Property3", ERROR_3)
            };
            var validationResult = new ValidationResult(validationFailures);

            var result = validationResult.ToResult();

            result.Should().BeFailure();
            result.Errors.Should().HaveCount(3);
            result.Errors.Select(e => e.Message).Should().BeEquivalentTo([ERROR_1, ERROR_2, ERROR_3]);
        }

        [Fact]
        public void GivenMultipleValidationFailuresForSamePropertyShouldIncludeAllErrors()
        {
            const string PROPERTY_NAME = "Email";
            const string ERROR_1 = "Email is required";
            const string ERROR_2 = "Email must be valid";

            var validationFailures = new[]
            {
                new ValidationFailure(PROPERTY_NAME, ERROR_1),
                new ValidationFailure(PROPERTY_NAME, ERROR_2)
            };
            var validationResult = new ValidationResult(validationFailures);

            var result = validationResult.ToResult();

            result.Should().BeFailure();
            result.Errors.Should().HaveCount(2);
            result.Errors.Select(e => e.Message).Should().BeEquivalentTo([ERROR_1, ERROR_2]);
        }
    }
}