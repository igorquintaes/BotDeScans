using BotDeScans.App.Services;
using BotDeScans.App.Services.Initializations.Factories;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.UnitTests.Specs.Services;

public class MangaDexServiceTests : UnitTest
{
    private readonly MangaDexService service;

    public MangaDexServiceTests()
    {
        fixture.FreezeFake<IMangaDex>();
        fixture.FreezeFake<IConfiguration>();
        fixture.FreezeFake<MangaDexAccessToken>();

        service = fixture.Create<MangaDexService>();
    }

    public class ClearPendingUploadsAsync : MangaDexServiceTests
    {
        public ClearPendingUploadsAsync()
        {
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
                .Get(fixture.FreezeFake<MangaDexAccessToken>().Value))
                .Returns(getResponse);

            A.CallTo(() => fixture
                .FreezeFake<IMangaDexUploadService>()
                .Abandon(getResponse.Data.Id, fixture.FreezeFake<MangaDexAccessToken>().Value))
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
