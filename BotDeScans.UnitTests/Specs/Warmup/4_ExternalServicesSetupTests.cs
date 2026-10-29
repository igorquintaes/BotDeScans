using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Warmup;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.UnitTests.Specs.Warmup;

public class ExternalServicesSetupTests : UnitTest
{
    public class RunAsync : ExternalServicesSetupTests
    {
        private readonly IServiceScope serviceScope;

        public RunAsync()
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton(fixture.FreezeFake<SetupClientsService>())
                .BuildServiceProvider();

            serviceScope = serviceProvider.CreateScope();

            A.CallTo(() => fixture
                .FreezeFake<SetupClientsService>()
                .SetupAsync(cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await ExternalServicesSetup.RunAsync(serviceScope, cancellationToken);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallSetupAsync()
        {
            await ExternalServicesSetup.RunAsync(serviceScope, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<SetupClientsService>()
                .SetupAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSetupErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "setup error";

            A.CallTo(() => fixture
                .FreezeFake<SetupClientsService>()
                .SetupAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await ExternalServicesSetup.RunAsync(serviceScope, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExceptionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "exceptional error";

            A.CallTo(() => fixture
                .FreezeFake<SetupClientsService>()
                .SetupAsync(cancellationToken))
                .Throws(new Exception(ERROR_MESSAGE));

            var result = await ExternalServicesSetup.RunAsync(serviceScope, cancellationToken);

            result.Should().BeFailure().And.HaveError("An exceptional error occurred during External Services setup.");

            result.Errors.Should().ContainSingle();
            result.Errors.Single().Should().BeOfType<ExceptionalError>();
            result.Errors.Single().As<ExceptionalError>().Exception.Message.Should().Be(ERROR_MESSAGE);
        }
    }
}
