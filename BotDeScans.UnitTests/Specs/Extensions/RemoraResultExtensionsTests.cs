using BotDeScans.App.Extensions;
using Remora.Results;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class RemoraResultExtensionsTests : UnitTest
{
    public class ToFluentResult : RemoraResultExtensionsTests
    {
        [Fact]
        public void GivenSuccessResultShouldReturnOkWithFactoryValue()
        {
            const string VALUE = "value";

            var remoraResult = Result.FromSuccess();

            var fluentResult = remoraResult.ToFluentResult(() => VALUE);

            fluentResult.Should().BeSuccess().And.HaveValue(VALUE);
        }

        [Fact]
        public void GivenFailedResultShouldReturnFailWithDefaultAndErrorMessages()
        {
            const string ERROR_MESSAGE = "error";
            var remoraResult = Result.FromError(new InvalidOperationError(ERROR_MESSAGE));

            var fluentResult = remoraResult.ToFluentResult(() => "unused");

            fluentResult.Should().BeFailure()
                        .And.HaveError(ERROR_MESSAGE)
                        .And.HaveError("Error in Discord communication.")
                        .Which.Errors.Should().HaveCount(2);
        }

        [Fact]
        public void GivenSuccessResultShouldInvokeFactory()
        {
            const string VALUE = "value";

            var invoked = false;
            var remoraResult = Result.FromSuccess();

            var fluentResult = remoraResult.ToFluentResult(() =>
            {
                invoked = true;
                return VALUE;
            });

            invoked.Should().BeTrue();
            fluentResult.Should().BeSuccess().And.HaveValue(VALUE);
        }

        [Fact]
        public void GivenFailedResultShouldNotInvokeFactory()
        {
            const string ERROR_MESSAGE = "error";
            var invoked = false;
            var remoraResult = Result.FromError(new InvalidOperationError(ERROR_MESSAGE));

            var fluentResult = remoraResult.ToFluentResult(() =>
            {
                invoked = true;
                return "unused";
            });

            invoked.Should().BeFalse();
            fluentResult.Should().BeFailure()
                        .And.HaveError(ERROR_MESSAGE)
                        .And.HaveError("Error in Discord communication.")
                        .Which.Errors.Should().HaveCount(2);
        }
    }
}
