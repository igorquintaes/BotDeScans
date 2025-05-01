using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Initializations;
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
                .GetService(typeof(FakeClientFactory)))
                .Returns(fixture.FreezeFake<FakeClientFactory>());

            A.CallTo(() => fixture
                .FreezeFake<IServiceProvider>()
                .GetService(typeof(IValidator<FakeClientFactory>)))
                .Returns(fixture.FreezeFake<IValidator<FakeClientFactory>>());

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactory>()
                .ExpectedInPublishFeature)
                .Returns(true);
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
                .FreezeFake<IValidator<FakeClientFactory>>()
                .ValidateAsync(fixture.FreezeFake<FakeClientFactory>(), cancellationToken))
                .Returns(new ValidationResult([new ValidationFailure("prop", ERROR_MESSAGE)]));

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToCreateAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some-error";
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactory>()
                .CreateAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenErrorToHealthCheckAsyncShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some-error";
            var client = new object();
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactory>()
                .CreateAsync(cancellationToken))
                .Returns(Result.Ok(client));

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactory>()
                .HealthCheckAsync(client, cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenClientNotEnabledForPublishShouldSkipItsSetup()
        {
            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactory>()
                .ExpectedInPublishFeature)
                .Returns(false);

            var result = await service.SetupAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactory>()
                .CreateAsync(A<CancellationToken>.Ignored))
                .MustNotHaveHappened();

            A.CallTo(() => fixture
                .FreezeFake<FakeClientFactory>()
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

        public class FakeClientFactory : ClientFactory<object>
        {
            public override bool ExpectedInPublishFeature =>
                throw new NotImplementedException();

            public override Task<Result<object>> CreateAsync(CancellationToken cancellationToken) =>
                throw new NotImplementedException();

            public override Task<Result> HealthCheckAsync(object client, CancellationToken cancellationToken) =>
                throw new NotImplementedException();
        }
    }
}
