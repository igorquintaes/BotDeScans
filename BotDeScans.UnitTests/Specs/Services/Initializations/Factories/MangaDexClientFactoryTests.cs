using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services.Initializations.Factories;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.Factories;

public class MangaDexClientFactoryTests : UnitTest
{
    private readonly MangaDexClientFactory factory;

    public MangaDexClientFactoryTests()
    {
        fixture.FreezeFake<IMangaDex>();
        fixture.FreezeFake<IConfiguration>();

        factory = fixture.Create<MangaDexClientFactory>();
    }

    public class Enabled : MangaDexClientFactoryTests
    {
        [Fact]
        public void GivenExpectedStepInsideArrayShouldReturnTrue()
        {
            fixture.FreezeFakeConfiguration(
                key: "Settings:Publish:Steps",
                values: Enum
                    .GetValues<StepName>()
                    .Select(x => x.ToString()));

            factory.Enabled.Should().BeTrue();
        }

        [Fact]
        public void GivenNotExpectedStepShouldReturnFalse()
        {
            fixture.FreezeFakeConfiguration(
                key: "Settings:Publish:Steps",
                values: Enum
                    .GetValues<StepName>()
                    .Except([StepName.UploadMangadex])
                    .Select(x => x.ToString()));

            factory.Enabled.Should().BeFalse();
        }
    }

    public class CreateAsync : MangaDexClientFactoryTests
    {
        public CreateAsync()
        {
            var user = fixture.Create<string>();
            var pass = fixture.Create<string>();
            var clientId = fixture.Create<string>();
            var clientSecret = fixture.Create<string>();

            fixture.FreezeFakeConfiguration("Mangadex:Username", user);
            fixture.FreezeFakeConfiguration("Mangadex:Password", pass);
            fixture.FreezeFakeConfiguration("Mangadex:ClientId", clientId);
            fixture.FreezeFakeConfiguration("Mangadex:ClientSecret", clientSecret);

            fixture.Inject(new TokenResult
            {
                ExpiresIn = 100d,
                AccessToken = fixture.Create<string>()
            });

            A.CallTo(() => fixture
                .FreezeFake<IMangaDex>().Auth)
                .Returns(fixture.FreezeFake<IMangaDexAuthService>());

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexAuthService>()
                .Personal(clientId, clientSecret, user, pass))
                .Returns(fixture.Freeze<TokenResult>());
        }

        [Fact]
        public async Task GivenSuccessfulCreateShouldReturnSuccessResult()
        {
            var expectedAccessToken = fixture.Freeze<TokenResult>().AccessToken;
            var expectedResultValue = new MangaDexAccessToken(expectedAccessToken);

            var result = await factory.CreateAsync(cancellationToken);

            result.Should().BeSuccess();
            result.ValueOrDefault?.Should().BeEquivalentTo(expectedResultValue);
        }

        [Fact]
        public async Task GivenNullAuthResultShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IMangaDexAuthService>()
                .Personal(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored))
                .Returns(Task.FromResult(null as TokenResult)!);

            var result = await factory.CreateAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError("Unable to login in mangadex.");
        }

        [Fact]
        public async Task GivenNullAuthExpiresResultShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IMangaDexAuthService>()
                .Personal(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored))
                .Returns(new TokenResult
                {
                    ExpiresIn = null,
                    AccessToken = "valid"
                });

            var result = await factory.CreateAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError("Unable to login in mangadex.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GivenNullAuthAccessTokenResultShouldReturnFailResult(string? token)
        {
            A.CallTo(() => fixture
                .FreezeFake<IMangaDexAuthService>()
                .Personal(
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored,
                    A<string>.Ignored))
                .Returns(new TokenResult
                {
                    ExpiresIn = 100d,
                    AccessToken = token!
                });

            var result = await factory.CreateAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError("Unable to login in mangadex.");
        }
    }

    public class HealthCheckAsync : MangaDexClientFactoryTests
    {
        private readonly MangaDexAccessToken mangaDexAccessToken;

        public HealthCheckAsync()
        {
            mangaDexAccessToken = fixture.Create<MangaDexAccessToken>();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDex>().User)
                .Returns(fixture.FreezeFake<IMangaDexUserService>());

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUserService>()
                .Me(mangaDexAccessToken.Value))
                .Returns(new MangaDexRoot<User>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await factory.HealthCheckAsync(mangaDexAccessToken, cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorRecievedShouldReturnFailResult()
        {
            var error = new MangaDexError()
            {
                Status = 404,
                Title = "Not Found",
                Detail = "Success not found."
            };
            var errorMessage = $"{error.Status} {error.Title}: {error.Detail}";

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUserService>()
                .Me(mangaDexAccessToken.Value))
                .Returns(new MangaDexRoot<User>() { Errors = [error], Result = "error" });

            var result = await factory.HealthCheckAsync(mangaDexAccessToken, cancellationToken);

            result.Should().BeFailure().And.HaveError(errorMessage);
        }

        [Fact]
        public async Task GivenMultipleErrorsRecievedShouldReturnFailResult()
        {
            var firstError = new MangaDexError()
            {
                Status = 404,
                Title = "Not Found",
                Detail = "Success not found."
            };

            var secondError = new MangaDexError()
            {
                Status = 500,
                Title = "InternalServerError",
                Detail = "Something went wrong."
            };

            var firstErrorMessage = $"{firstError.Status} {firstError.Title}: {firstError.Detail}";
            var secondErrorMessage = $"{secondError.Status} {secondError.Title}: {secondError.Detail}";

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUserService>()
                .Me(mangaDexAccessToken.Value))
                .Returns(new MangaDexRoot<User>() { Errors = [firstError, secondError], Result = "error" });

            var result = await factory.HealthCheckAsync(mangaDexAccessToken, cancellationToken);

            result.Should().BeFailure()
                .And.HaveError(firstErrorMessage)
                .And.HaveError(secondErrorMessage);
        }
    }
}