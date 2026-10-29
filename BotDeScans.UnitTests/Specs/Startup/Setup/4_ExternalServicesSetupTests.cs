using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Startup.Setup;
using FluentResults;

namespace BotDeScans.UnitTests.Specs.Startup.Setup;

public class ClientsSetupTests : UnitTest
{
    public class RunAsync : ClientsSetupTests
    {
        private readonly ClientsSetup clientsSetup;

        public RunAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<SetupClientsService>()
                .SetupAsync(cancellationToken))
                .Returns(Result.Ok());

            clientsSetup = fixture.Create<ClientsSetup>();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await clientsSetup.ExecuteAsync(cancellationToken);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallSetupAsync()
        {
            var result = await clientsSetup.ExecuteAsync(cancellationToken);

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

            var result = await clientsSetup.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
