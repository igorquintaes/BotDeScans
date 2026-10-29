using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Startup.Setup;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Startup.Setup;

public class StepsSetupTests : UnitTest
{
    public class Run : StepsSetupTests
    {
        private readonly StepsSetup setup;

        public Run()
        {
            A.CallTo(() => fixture
                .FreezeFake<SetupStepsService>()
                .Setup())
                .Returns(Result.Ok());

            setup = fixture.Create<StepsSetup>();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await setup.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallSetup()
        {
            var result = await setup.ExecuteAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<SetupStepsService>()
                .Setup())
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSetupErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "setup error";

            A.CallTo(() => fixture
                .FreezeFake<SetupStepsService>()
                .Setup())
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await setup.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExceptionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "setup exception";

            A.CallTo(() => fixture
                .FreezeFake<SetupStepsService>()
                .Setup())
                .Throws(new Exception(ERROR_MESSAGE));

            var result = await setup.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError("An exceptional error occurred during Steps setup.");

            result.Errors.Should().ContainSingle();
            result.Errors.Single().Should().BeOfType<ExceptionalError>();
            result.Errors.Single().As<ExceptionalError>().Exception.Message.Should().Be(ERROR_MESSAGE);
        }
    }
}
