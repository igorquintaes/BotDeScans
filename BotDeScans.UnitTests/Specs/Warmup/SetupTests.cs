using BotDeScans.App.Warmup;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.UnitTests.Specs.Warmup;

public class SetupTests : UnitTest
{
    public class Run : SetupTests
    {
        private readonly IServiceProvider serviceProvider;
        private readonly SetupService setupService;

        public Run()
        {
            var databaseSetup = fixture.FreezeFake<IDatabaseSetup>();
            var discordSetup = fixture.FreezeFake<IDiscordSetup>();
            var stepsSetup = fixture.FreezeFake<IStepsSetup>();
            var externalServicesSetup = fixture.FreezeFake<IExternalServicesSetup>();

            serviceProvider = new ServiceCollection()
                .AddSingleton(databaseSetup)
                .AddSingleton(discordSetup)
                .AddSingleton(stepsSetup)
                .AddSingleton(externalServicesSetup)
                .BuildServiceProvider();

            setupService = new SetupService(databaseSetup, discordSetup, stepsSetup, externalServicesSetup);

            A.CallTo(() => databaseSetup.RunAsync(A<AsyncServiceScope>._, cancellationToken))
                .Returns(Result.Ok());

            A.CallTo(() => discordSetup.RunAsync(A<IServiceScope>._, cancellationToken))
                .Returns(Result.Ok());

            A.CallTo(() => stepsSetup.Run(A<IServiceScope>._, cancellationToken))
                .Returns(Result.Ok());

            A.CallTo(() => externalServicesSetup.RunAsync(A<IServiceScope>._, cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await setupService.Run(serviceProvider, cancellationToken);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallDatabaseSetup()
        {
            await setupService.Run(serviceProvider, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<IDatabaseSetup>()
                .RunAsync(A<AsyncServiceScope>._, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallDiscordSetup()
        {
            await setupService.Run(serviceProvider, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<IDiscordSetup>()
                .RunAsync(A<IServiceScope>._, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallStepsSetup()
        {
            await setupService.Run(serviceProvider, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<IStepsSetup>()
                .Run(A<IServiceScope>._, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallExternalServicesSetup()
        {
            await setupService.Run(serviceProvider, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<IExternalServicesSetup>()
                .RunAsync(A<IServiceScope>._, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldHaveSuccessMessages()
        {
            var result = await setupService.Run(serviceProvider, cancellationToken);

            result.Successes.Should().HaveCount(4);
            result.Successes.Select(s => s.Message).Should().Contain([
                "Database setup completed successfully.",
                "Discord setup completed successfully.",
                "Steps setup completed successfully.",
                "External Services setup completed successfully."
            ]);
        }

        [Fact]
        public async Task GivenDatabaseSetupErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "database error";

            A.CallTo(() => fixture.FreezeFake<IDatabaseSetup>()
                .RunAsync(A<AsyncServiceScope>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await setupService.Run(serviceProvider, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenDatabaseSetupErrorShouldNotCallDiscordSetup()
        {
            A.CallTo(() => fixture.FreezeFake<IDatabaseSetup>()
                .RunAsync(A<AsyncServiceScope>._, cancellationToken))
                .Returns(Result.Fail("database error"));

            await setupService.Run(serviceProvider, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<IDiscordSetup>()
                .RunAsync(A<IServiceScope>._, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenDiscordSetupErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "discord error";

            A.CallTo(() => fixture.FreezeFake<IDiscordSetup>()
                .RunAsync(A<IServiceScope>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await setupService.Run(serviceProvider, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenDiscordSetupErrorShouldNotCallStepsSetup()
        {
            A.CallTo(() => fixture.FreezeFake<IDiscordSetup>()
                .RunAsync(A<IServiceScope>._, cancellationToken))
                .Returns(Result.Fail("discord error"));

            await setupService.Run(serviceProvider, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<IStepsSetup>()
                .Run(A<IServiceScope>._, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenStepsSetupErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "steps error";

            A.CallTo(() => fixture.FreezeFake<IStepsSetup>()
                .Run(A<IServiceScope>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await setupService.Run(serviceProvider, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenStepsSetupErrorShouldNotCallExternalServicesSetup()
        {
            A.CallTo(() => fixture.FreezeFake<IStepsSetup>()
                .Run(A<IServiceScope>._, cancellationToken))
                .Returns(Result.Fail("steps error"));

            await setupService.Run(serviceProvider, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<IExternalServicesSetup>()
                .RunAsync(A<IServiceScope>._, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenExternalServicesSetupErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "external services error";

            A.CallTo(() => fixture.FreezeFake<IExternalServicesSetup>()
                .RunAsync(A<IServiceScope>._, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await setupService.Run(serviceProvider, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
