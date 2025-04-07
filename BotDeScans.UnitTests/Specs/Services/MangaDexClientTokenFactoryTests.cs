using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Services;
using BotDeScans.UnitTests.Extensions;
using Google.Apis.Auth.OAuth2;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.UnitTests.Specs.Services;

public class MangaDexClientTokenFactoryTests : UnitTest
{
    private readonly MangaDexClientTokenFactory factory;

    public MangaDexClientTokenFactoryTests()
    {
        fixture.FreezeFake<IMangaDex>();
        fixture.FreezeFake<IConfiguration>();

        factory = fixture.Create<MangaDexClientTokenFactory>();
    }

    public class ExpectedInPublishFeature : MangaDexClientTokenFactoryTests
    {
        [Fact]
        public void GivenExpectedStepInsideArrayShouldReturnTrue()
        {
            fixture.FreezeFakeConfiguration(
                key: "Settings:Publish:Steps",
                values: Enum
                    .GetValues<StepEnum>()
                    .Select(x => x.ToString()));

            factory.ExpectedInPublishFeature.Should().BeTrue();
        }

        [Fact]
        public void GivenNotExpectedStepShouldReturnFalse()
        {
            fixture.FreezeFakeConfiguration(
                key: "Settings:Publish:Steps",
                values: Enum
                    .GetValues<StepEnum>()
                    .Except([StepEnum.UploadMangadex])
                    .Select(x => x.ToString()));

            factory.ExpectedInPublishFeature.Should().BeFalse();
        }
    }

    public class ValidateConfiguration : MangaDexClientTokenFactoryTests
    {
        public ValidateConfiguration()
        {
            fixture.FreezeFakeConfiguration("Mangadex:Username", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Mangadex:Password", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Mangadex:ClientId", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Mangadex:ClientSecret", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Mangadex:GroupId", fixture.Create<string>());
        }

        [Fact]
        public void GivenValidConfigurationShouldReturnSuccessResult()
        {
            var result = factory.ValidateConfiguration();

            result.Should().BeSuccess();
        }

        [Theory]
        [InlineData("Mangadex:Username", "")]
        [InlineData("Mangadex:Username", " ")]
        [InlineData("Mangadex:Username", null)]
        [InlineData("Mangadex:Password", "")]
        [InlineData("Mangadex:Password", " ")]
        [InlineData("Mangadex:Password", null)]
        [InlineData("Mangadex:ClientId", "")]
        [InlineData("Mangadex:ClientId", " ")]
        [InlineData("Mangadex:ClientId", null)]
        [InlineData("Mangadex:ClientSecret", "")]
        [InlineData("Mangadex:ClientSecret", " ")]
        [InlineData("Mangadex:ClientSecret", null)]
        [InlineData("Mangadex:GroupId", "")]
        [InlineData("Mangadex:GroupId", " ")]
        [InlineData("Mangadex:GroupId", null)]
        public void GivenSingleInvalidConfigurationShouldReturnFailResult(string key, string? value)
        {
            fixture.FreezeFakeConfiguration(key, value);

            var result = factory.ValidateConfiguration();

            result.Should().BeFailure().And.HaveError($"'{key}': value not found in config.json.");
        }

        [Fact]
        public void GivenListOfInvalisConfigurationsShouldReturnFailResult()
        {
            fixture.FreezeFakeConfiguration("Mangadex:Username", string.Empty);
            fixture.FreezeFakeConfiguration("Mangadex:Password", string.Empty);
            fixture.FreezeFakeConfiguration("Mangadex:ClientId", string.Empty);
            fixture.FreezeFakeConfiguration("Mangadex:ClientSecret", string.Empty);
            fixture.FreezeFakeConfiguration("Mangadex:GroupId", string.Empty);

            var result = factory.ValidateConfiguration();

            result.Should().BeFailure()
                .And.HaveError($"'Mangadex:Username': value not found in config.json.")
                .And.HaveError($"'Mangadex:Password': value not found in config.json.")
                .And.HaveError($"'Mangadex:ClientId': value not found in config.json.")
                .And.HaveError($"'Mangadex:ClientSecret': value not found in config.json.")
                .And.HaveError($"'Mangadex:GroupId': value not found in config.json.");
        }
    }

    public class CreateAsync : MangaDexClientTokenFactoryTests
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

    public class HealthCheckAsync : MangaDexClientTokenFactoryTests
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