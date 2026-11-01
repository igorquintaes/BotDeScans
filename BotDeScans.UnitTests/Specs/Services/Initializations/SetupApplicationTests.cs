using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Services.Initializations.Setups;
using FluentResults;
using Microsoft.Extensions.Hosting;

namespace BotDeScans.UnitTests.Specs.Services.Initializations;

public class SetupApplicationTests : UnitTest
{
    public class StartAsync : SetupApplicationTests
    {
        private readonly SetupApplication setup;

        public StartAsync()
        {
            fixture.FreezeFake<IHostApplicationLifetime>();

            fixture.Inject<IEnumerable<ISetup>>([
                fixture.FreezeFake<ClientsSetup>(),
                fixture.FreezeFake<DatabaseSetup>(),
                fixture.FreezeFake<DiscordSlashCommandsSetup>(),
                fixture.FreezeFake<DiscordValidatorSetup>(),
                fixture.FreezeFake<StepsSetup>()
            ]);

            A.CallTo(() => fixture
                .FreezeFake<ClientsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<DatabaseSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<DiscordSlashCommandsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<DiscordValidatorSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<StepsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(GoogleDriveInitializer)))
                .Returns(fixture.FreezeFake<GoogleDriveInitializer>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveInitializer>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Ok());

            setup = fixture.Create<SetupApplication>();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldContinueAppExecution()
        {
            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IHostApplicationLifetime>()
                .StopApplication())
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallClientsSetup()
        {
            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<ClientsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallDatabaseSetup()
        {
            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DatabaseSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallDiscordSlashCommandsSetup()
        {
            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DiscordSlashCommandsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallDiscordValidatorSetup()
        {
            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DiscordValidatorSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallStepsSetup()
        {
            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<StepsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenClientsSetupErrorShouldStopApplication()
        {
            A.CallTo(() => fixture
                .FreezeFake<ClientsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Fail("error"));

            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IHostApplicationLifetime>()
                .StopApplication())
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenDatabaseSetupErrorShouldStopApplication()
        {
            A.CallTo(() => fixture
                .FreezeFake<DatabaseSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Fail("error"));

            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IHostApplicationLifetime>()
                .StopApplication())
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenDiscordSlashCommandsSetupErrorShouldStopApplication()
        {
            A.CallTo(() => fixture
                .FreezeFake<DiscordSlashCommandsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Fail("error"));

            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IHostApplicationLifetime>()
                .StopApplication())
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenDiscordValidatorSetupErrorShouldStopApplication()
        {
            fixture.Inject<IEnumerable<ISetup>>([
                fixture.FreezeFake<DiscordValidatorSetup>()
            ]);

            A.CallTo(() => fixture
                .FreezeFake<DiscordValidatorSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Fail("error"));

            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IHostApplicationLifetime>()
                .StopApplication())
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenStepsSetupErrorShouldStopApplication()
        {
            A.CallTo(() => fixture
                .FreezeFake<StepsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Fail("error"));

            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IHostApplicationLifetime>()
                .StopApplication())
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessClientsSetupExecutionShouldCallGoogleDriveInitializer()
        {
            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveInitializer>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenFailClientsSetupExecutionShouldNotCallGoogleDriveInitializer()
        {
            A.CallTo(() => fixture
                .FreezeFake<ClientsSetup>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .Returns(Result.Fail("error"));

            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveInitializer>()
                .ExecuteAsync(A<CancellationToken>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenMissingClientsSetupShouldCallGoogleDriveInitializer()
        {
            fixture.Inject<IEnumerable<ISetup>>([
                fixture.FreezeFake<DatabaseSetup>(),
                fixture.FreezeFake<DiscordSlashCommandsSetup>(),
                fixture.FreezeFake<DiscordValidatorSetup>(),
                fixture.FreezeFake<StepsSetup>()
            ]);

            await setup.StartAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveInitializer>()
                .ExecuteAsync(cancellationToken))
                .MustNotHaveHappened();
        }
    }
}