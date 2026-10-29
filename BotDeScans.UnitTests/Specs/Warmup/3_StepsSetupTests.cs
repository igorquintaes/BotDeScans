using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Warmup;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.UnitTests.Specs.Warmup;

public class StepsSetupTests : UnitTest
{
    public class Run : StepsSetupTests, IDisposable
    {
        private readonly IServiceScope serviceScope;

        public Run()
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton(fixture.FreezeFake<SetupStepsService>())
                .BuildServiceProvider();

            A.CallTo(() => fixture
                .FreezeFake<SetupStepsService>()
                .Setup())
                .Returns(Result.Ok());

            serviceScope = serviceProvider.CreateScope();
        }

        [Fact]
        public void GivenSuccessfulExecutionShouldReturnSuccessResult()
        {

            var result = StepsSetup.Run(serviceScope, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public void GivenSuccessfulExecutionShouldCallSetup()
        {
            StepsSetup.Run(serviceScope, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<SetupStepsService>()
                .Setup())
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void GivenSetupErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "setup error";

            A.CallTo(() => fixture
                .FreezeFake<SetupStepsService>()
                .Setup())
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = StepsSetup.Run(serviceScope, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public void GivenExceptionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "setup exception";

            A.CallTo(() => fixture
                .FreezeFake<SetupStepsService>()
                .Setup())
                .Throws(new Exception(ERROR_MESSAGE));

            var result = StepsSetup.Run(serviceScope, cancellationToken);

            result.Should().BeFailure().And.HaveError("An exceptional error occurred during Steps setup.");

            result.Errors.Should().ContainSingle();
            result.Errors.Single().Should().BeOfType<ExceptionalError>();
            result.Errors.Single().As<ExceptionalError>().Exception.Message.Should().Be(ERROR_MESSAGE);
        }

        public void Dispose()
        {
            serviceScope.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
