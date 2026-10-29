using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Warmup;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.UnitTests.Specs.Warmup;

public class DiscordSetupTests : UnitTest
{
    public class RunAsync : DiscordSetupTests
    {
        private readonly IServiceScope serviceScope;

        public RunAsync()
        {
            var setupDiscordService = fixture.FreezeFake<SetupDiscordService>();
            var validator = fixture.FreezeFake<IValidator<SetupDiscordService>>();

            var serviceProvider = new ServiceCollection()
                .AddSingleton(setupDiscordService)
                .AddSingleton(validator)
                .BuildServiceProvider();

            serviceScope = serviceProvider.CreateScope();

            A.CallTo(() => validator
                .ValidateAsync(setupDiscordService, cancellationToken))
                .Returns(new ValidationResult());

            A.CallTo(() => setupDiscordService
                .SetupAsync(cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await DiscordSetup.RunAsync(serviceScope, cancellationToken);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallValidationSetup()
        {
            await DiscordSetup.RunAsync(serviceScope, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IValidator<SetupDiscordService>>()
                .ValidateAsync(fixture.FreezeFake<SetupDiscordService>(), cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallCommandsSetup()
        {
            await DiscordSetup.RunAsync(serviceScope, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<SetupDiscordService>()
                .SetupAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenValidationErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "validation error";

            A.CallTo(() => fixture
                .FreezeFake<IValidator<SetupDiscordService>>()
                .ValidateAsync(fixture.FreezeFake<SetupDiscordService>(), cancellationToken))
                .Returns(new ValidationResult([new ValidationFailure("prop", ERROR_MESSAGE)]));

            var result = await DiscordSetup.RunAsync(serviceScope, cancellationToken);

            result.Should()
                .BeFailure().And
                .HaveError(ERROR_MESSAGE).Which
                .Errors.Should().ContainSingle().Which
                .Metadata.Should().Contain("PropertyName", "prop");
        }

        [Fact]
        public async Task GivenValidationErrorShouldNotCallCommandsSetup()
        {
            const string ERROR_MESSAGE = "validation error";

            A.CallTo(() => fixture
                .FreezeFake<IValidator<SetupDiscordService>>()
                .ValidateAsync(fixture.FreezeFake<SetupDiscordService>(), cancellationToken))
                .Returns(new ValidationResult([new ValidationFailure("prop", ERROR_MESSAGE)]));

            await DiscordSetup.RunAsync(serviceScope, cancellationToken);

            A.CallTo(() => fixture.FreezeFake<SetupDiscordService>()
                .SetupAsync(cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenCommandsSetupErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "commands setup error";

            A.CallTo(() => fixture.FreezeFake<SetupDiscordService>()
                .SetupAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await DiscordSetup.RunAsync(serviceScope, cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenExceptionShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "exceptional error";

            A.CallTo(() => fixture
                .FreezeFake<IValidator<SetupDiscordService>>()
                .ValidateAsync(fixture.FreezeFake<SetupDiscordService>(), cancellationToken))
                .Throws(new Exception(ERROR_MESSAGE));

            var result = await DiscordSetup.RunAsync(serviceScope, cancellationToken);

            result.Should().BeFailure().And.HaveError("An exceptional error occurred during Discord setup.");

            result.Errors.Should().ContainSingle();
            result.Errors.Single().Should().BeOfType<ExceptionalError>();
            result.Errors.Single().As<ExceptionalError>().Exception.Message.Should().Be(ERROR_MESSAGE);
        }
    }
}
