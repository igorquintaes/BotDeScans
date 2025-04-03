using BotDeScans.App.Services;
using BotDeScans.UnitTests.Extensions;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace BotDeScans.UnitTests.Specs.Services;

public class MangaDexServiceTests : UnitTest
{
    private readonly MangaDexService service;

    public MangaDexServiceTests()
    {
        fixture.FreezeFake<IMangaDex>();
        fixture.FreezeFake<IConfiguration>();

        service = fixture.Create<MangaDexService>();
    }

    public class LoginAsync : MangaDexServiceTests
    {
        private readonly TokenResult tokenResult;

        public LoginAsync()
        {
            var username = fixture.Create<string>();
            var password = fixture.Create<string>();
            var clientId = fixture.Create<string>();
            var clientSecret = fixture.Create<string>();

            fixture.FreezeFakeConfiguration("Mangadex:Username", username);
            fixture.FreezeFakeConfiguration("Mangadex:Password", password);
            fixture.FreezeFakeConfiguration("Mangadex:ClientId", clientId);
            fixture.FreezeFakeConfiguration("Mangadex:ClientSecret", clientSecret);

            tokenResult = fixture
                .Build<TokenResult>()
                .With(x => x.ExpiresIn, 3600)
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDex>().Auth)
                .Returns(fixture.FreezeFake<IMangaDexAuthService>());

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexAuthService>()
                .Personal(clientId, clientSecret, username, password))
                .Returns(tokenResult);
        }

        [Fact]
        public async Task GivenSuccessfulLoginShouldReturnSuccessResult()
        {
            var result = await service.LoginAsync();

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulLoginShouldSaveAccessTokenInsideClass()
        {
            await service.LoginAsync();

            var accessToken = service.GetType()
                .GetField("accessToken", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(service);

            accessToken.Should().Be(tokenResult.AccessToken);
        }

        [Fact]
        public async Task GivenNullReturnFromMangaDexShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IMangaDexAuthService>()
                .Personal(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult<TokenResult>(null!));

            var result = await service.LoginAsync();

            result.Should().BeFailure().And.HaveError("Unable to login in mangadex.");
        }

        [Fact]
        public async Task GivenNoneExpiriesInFromMangaDexShouldReturnFailResult()
        {
            tokenResult.ExpiresIn = null;

            var result = await service.LoginAsync();

            result.Should().BeFailure().And.HaveError("Unable to login in mangadex.");
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(-1d)]
        [InlineData(null)]
        public async Task GivenNotPositiveExpiresInFromMangaDexShouldReturnFailResult(double? expiresIn)
        {
            tokenResult.ExpiresIn = expiresIn;

            var result = await service.LoginAsync();

            result.Should().BeFailure().And.HaveError("Unable to login in mangadex.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GivenNotFilledAccessTokenFromMangaDexShouldReturnFailResult(string? accessToken)
        {
            tokenResult.AccessToken = accessToken!;

            var result = await service.LoginAsync();

            result.Should().BeFailure().And.HaveError("Unable to login in mangadex.");
        }
    }

    public class ClearPendingUploadsAsync : MangaDexServiceTests
    {
        public ClearPendingUploadsAsync()
        {
            var accessToken = fixture.Create<string>();

            service.GetType()
                .GetField("accessToken", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(service, accessToken);

            var getResponse = fixture
                .Build<MangaDexRoot<UploadSession>>()
                .With(x => x.Errors, [])
                .Create();

            var abandonResponse = fixture
                .Build<MangaDexRoot<UploadSession>>()
                .With(x => x.Errors, [])
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDex>().Upload)
                .Returns(fixture.FreezeFake<IMangaDexUploadService>());

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Get(accessToken))
                .Returns(getResponse);

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Abandon(getResponse.Data.Id, accessToken))
                .Returns(abandonResponse);
        }

        [Fact]
        public async Task GivenSuccessfulClearShouldReturnFailResult()
        {
            var result = await service.ClearPendingUploadsAsync();

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenNotFoundPendingUploadsShouldReturnSuccessResult()
        {
            var getResponse = fixture
                .Build<MangaDexRoot<UploadSession>>()
                .With(x => x.Errors, [new MangaDexError { Status = 404 }])
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Get(A<string>.Ignored))
                .Returns(getResponse);

            var result = await service.ClearPendingUploadsAsync();

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorToGetPendingUploadsShouldReturnFailResult()
        {
            var error = new MangaDexError
            {
                Status = 500,
                Title = "error-title",
                Detail = "error-detail"
            };

            var getResponse = fixture
                .Build<MangaDexRoot<UploadSession>>()
                .With(x => x.Result, "error")
                .With(x => x.Errors, [error])
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Get(A<string>.Ignored))
                .Returns(getResponse);

            var result = await service.ClearPendingUploadsAsync();

            result.Should().BeFailure().And.HaveError($"{error.Status} - {error.Title} - {error.Detail}");
        }

        [Fact]
        public async Task GivenErrorToAbandonPendingUploadsShouldReturnFailResult()
        {
            var error = new MangaDexError
            {
                Status = 500,
                Title = "error-title",
                Detail = "error-detail"
            };

            var abandonResponse = fixture
                .Build<MangaDexRoot>()
                .With(x => x.Errors, [error])
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Abandon(A<string>.Ignored, A<string>.Ignored))
                .Returns(abandonResponse);

            var result = await service.ClearPendingUploadsAsync();

            result.Should().BeFailure().And.HaveError($"{error.Status} - {error.Title} - {error.Detail}");
        }
    }

    public class UploadChapterAsync : MangaDexServiceTests
    {

    }

    public class GetTitleIdFromUrl : MangaDexServiceTests
    {

    }
}
