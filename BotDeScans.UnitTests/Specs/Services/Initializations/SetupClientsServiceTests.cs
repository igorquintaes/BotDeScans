using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Services.Initializations.Factories.Base;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;

namespace BotDeScans.UnitTests.Specs.Services.Initializations;

public class SetupClientsServiceTests : UnitTest
{
    private readonly SetupClientsService service;

    public SetupClientsServiceTests()
    {
        fixture.FreezeFake<IServiceProvider>();

        service = fixture.Create<SetupClientsService>();
    }

    public class SetupAsync : SetupClientsServiceTests
    {
        public SetupAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(GoogleDriveSettingsService)))
                .Returns(fixture.FreezeFake<GoogleDriveSettingsService>());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(FakeClientFactoryA)))
                .Returns(fixture.FreezeFake<FakeClientFactoryA>());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(FakeClientFactoryB)))
                .Returns(fixture.FreezeFake<FakeClientFactoryB>());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(IValidator<FakeClientFactoryA>)))
                .Returns(fixture.FreezeFake<IValidator<FakeClientFactoryA>>());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(IValidator<FakeClientFactoryB>)))
                .Returns(fixture.FreezeFake<IValidator<FakeClientFactoryA>>());

            A.CallTo(() => fixture
                .FreezeFake<IValidator<FakeClientFactoryA>>()
                .ValidateAsync(A<ValidationContext<IClientFactory>>.That.Matches(x =>
                    x.InstanceToValidate == fixture.FreezeFake<FakeClientFactoryA>()),
                    cancellationToken))
                .Returns(new ValidationResult());

            A.CallTo(() => fixture
                .FreezeFake<IValidator<FakeClientFactoryB>>()
                .ValidateAsync(A<ValidationContext<IClientFactory>>.That.Matches(x =>
                    x.InstanceToValidate == fixture.FreezeFake<FakeClientFactoryB>()),
                    cancellationToken))
                .Returns(new ValidationResult());

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>().Enabled)
                .Returns(true);

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryB>().Enabled)
                .Returns(false);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnOkResult()
        {
            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenValidationErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some-error";
            A.CallTo(() => fixture
                .FreezeFake<IValidator<FakeClientFactoryA>>()
                .ValidateAsync(A<ValidationContext<IClientFactory>>.That.Matches(x =>
                    x.InstanceToValidate == fixture.FreezeFake<FakeClientFactoryA>()),
                    cancellationToken))
                .Returns(new ValidationResult([new ValidationFailure("prop", ERROR_MESSAGE)]));

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure()
                .And.HaveError(ERROR_MESSAGE)
                .And.HaveError($"Validation failed for {nameof(FakeClientFactoryA)}Proxy");
        }

        [Fact]
        public async Task GivenErrorToCreateAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some-error";
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .CreateAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure()
                .And.HaveError(ERROR_MESSAGE)
                .And.HaveError($"Object creation failed for {nameof(FakeClientFactoryA)}Proxy");
        }

        [Fact]
        public async Task GivenExceptionToCreateAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some-error";

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .CreateAsync(cancellationToken))
                .ThrowsAsync(new InvalidOperationException(ERROR_MESSAGE));

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure()
                .And.HaveError($"Failed to create a client of type Object.")
                .And.HaveError($"Object creation failed for {nameof(FakeClientFactoryA)}Proxy");

            result.Errors.Should().HaveCount(2);
            result.Errors[0].Reasons.Should().HaveCount(1);
            result.Errors[0].Reasons.FirstOrDefault()?.Should().BeOfType<ExceptionalError>();
            result.Errors[0].Reasons.FirstOrDefault()?.As<ExceptionalError?>()?.Exception.Should().BeOfType<InvalidOperationException>();
            result.Errors[0].Reasons.FirstOrDefault()?.As<ExceptionalError?>()?.Exception.Message.Should().Be(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToHealthCheckAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some-error";
            var client = new object();
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .CreateAsync(cancellationToken))
                .Returns(Result.Ok(client));

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .HealthCheckAsync(client, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure()
                .And.HaveError(ERROR_MESSAGE)
                .And.HaveError($"Health check failed for {nameof(FakeClientFactoryA)}Proxy");
        }

        [Fact]
        public async Task GivenClientNotEnabledForPublishShouldSkipItsSetup()
        {
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .Enabled)
                .Returns(false);

            var result = await service.SetupAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .CreateAsync(A<CancellationToken>.Ignored))
                .MustNotHaveHappened();

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .HealthCheckAsync(A<object>.Ignored, A<CancellationToken>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorToInitializeGoogleDriveShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some-error";
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveSettingsService>()
                .SetUpBaseFolderAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenMultipleClientsShouldAggregateSetupResults()
        {
            // FakeClientFactoryA
            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(FakeClientFactoryA)))
                .Returns(fixture.FreezeFake<FakeClientFactoryA>());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(IValidator<FakeClientFactoryA>)))
                .Returns(fixture.FreezeFake<IValidator<FakeClientFactoryA>>());

            A.CallTo(() => fixture
                .FreezeFake<IValidator<FakeClientFactoryA>>()
                .ValidateAsync(A<ValidationContext<IClientFactory>>.That.Matches(x =>
                    x.InstanceToValidate == fixture.FreezeFake<FakeClientFactoryA>()),
                    cancellationToken))
                .Returns(new ValidationResult());

            const string CREATE_SUCCESS_MSG_FACTORY_A = "create-success-a";
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .CreateAsync(cancellationToken))
                .Returns(Result.Ok().WithSuccess(CREATE_SUCCESS_MSG_FACTORY_A));

            const string HEALTH_SUCCESS_MSG_FACTORY_A = "health-success-a";
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryA>()
                .HealthCheckAsync(A<object>.Ignored, cancellationToken))
                .Returns(Result.Ok().WithSuccess(HEALTH_SUCCESS_MSG_FACTORY_A));

            // FakeClientFactoryB
            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(FakeClientFactoryB)))
                .Returns(fixture.FreezeFake<FakeClientFactoryB>());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(IValidator<FakeClientFactoryB>)))
                .Returns(fixture.FreezeFake<IValidator<FakeClientFactoryB>>());

            A.CallTo(() => fixture
                .FreezeFake<IValidator<FakeClientFactoryB>>()
                .ValidateAsync(A<ValidationContext<IClientFactory>>.That.Matches(x =>
                    x.InstanceToValidate == fixture.FreezeFake<FakeClientFactoryB>()),
                    cancellationToken))
                .Returns(new ValidationResult());

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryB>().Enabled)
                .Returns(true);

            const string CREATE_SUCCESS_MSG_FACTORY_B = "create-success-b";
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryB>()
                .CreateAsync(cancellationToken))
                .Returns(Result.Ok().WithSuccess(CREATE_SUCCESS_MSG_FACTORY_B));

            const string HEALTH_ERROR_MSG_FACTORY_B = "health-error-b";
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactoryB>()
                .HealthCheckAsync(A<object>.Ignored, cancellationToken))
                .Returns(Result.Fail(HEALTH_ERROR_MSG_FACTORY_B));

            // Act
            var result = await service.SetupAsync(cancellationToken);

            // Assert
            result.Should().BeFailure()
                .And.HaveReason(CREATE_SUCCESS_MSG_FACTORY_A)
                .And.HaveReason(HEALTH_SUCCESS_MSG_FACTORY_A)
                .And.HaveReason(CREATE_SUCCESS_MSG_FACTORY_B)
                .And.HaveError(HEALTH_ERROR_MSG_FACTORY_B)
                .And.HaveError($"Health check failed for {nameof(FakeClientFactoryB)}Proxy");
        }

        public class FakeClientFactoryA : ClientFactory<object>
        {
            public override bool Enabled =>
                throw new NotImplementedException();

            public override Task<Result<object>> CreateAsync(CancellationToken cancellationToken) =>
                throw new NotImplementedException();

            public override Task<Result> HealthCheckAsync(object client, CancellationToken cancellationToken) =>
                throw new NotImplementedException();
        }

        public class FakeClientFactoryB : ClientFactory<object>
        {
            public override bool Enabled =>
                throw new NotImplementedException();

            public override Task<Result<object>> CreateAsync(CancellationToken cancellationToken) =>
                throw new NotImplementedException();

            public override Task<Result> HealthCheckAsync(object client, CancellationToken cancellationToken) =>
                throw new NotImplementedException();
        }
    }
}
