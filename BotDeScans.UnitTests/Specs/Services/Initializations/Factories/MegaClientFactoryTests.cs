using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations.Factories;
using CG.Web.MegaApiClient;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.Factories;

public class MegaClientFactoryTests : UnitTest
{
    private readonly MegaClientFactory factory;

    public MegaClientFactoryTests()
    {
        factory = fixture.FreezeFake<MegaClientFactory>(options => options
                         .CallsBaseMethods());

        typeof(MegaClientFactory)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(x => x.FieldType == typeof(IConfiguration))
            .SetValue(factory, fixture.FreezeFake<IConfiguration>());

    }

    public class Enabled : MegaClientFactoryTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(StepName.UploadPdfMega)]
        [InlineData(StepName.UploadZipMega)]
        public void GivenExpectedStepInsideArrayShouldReturnTrue(StepName? stepToExclude)
        {
            fixture.FreezeFakeConfiguration(
                key: "Settings:Publish:Steps",
                values: Enum
                    .GetValues<StepName>()
                    .Where(x => x != stepToExclude)
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
                    .Except([StepName.UploadPdfMega, StepName.UploadZipMega])
                    .Select(x => x.ToString()));

            factory.Enabled.Should().BeFalse();
        }
    }

    public class CreateAsync : MegaClientFactoryTests
    {
        public CreateAsync()
        {
            var user = fixture.Create<string>();
            var pass = fixture.Create<string>();

            fixture.FreezeFakeConfiguration("Mega:User", user);
            fixture.FreezeFakeConfiguration("Mega:Pass", pass);

            A.CallTo(factory)
                .Where(call => call.Method.Name == "CreateClient")
                .WithReturnType<IMegaApiClient>()
                .Returns(fixture.FreezeFake<IMegaApiClient>());

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .IsLoggedIn)
                .Returns(true);
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await factory.CreateAsync(cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.FreezeFake<IMegaApiClient>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCallLoginAsync()
        {
            var user = fixture.Create<string>();
            var pass = fixture.Create<string>();

            fixture.FreezeFakeConfiguration("Mega:User", user);
            fixture.FreezeFakeConfiguration("Mega:Pass", pass);

            await factory.CreateAsync(cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .LoginAsync(user, pass, default))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToLoginShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .IsLoggedIn)
                .Returns(false);

            var result = await factory.CreateAsync(cancellationToken);

            result.Should()
                .BeFailure().And
                .HaveError("Unable to login on Mega. Check your user and pass, or if your account is blocked.");
        }
    }

    public class HealthCheckAsync : MegaClientFactoryTests
    {
        public HealthCheckAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .GetAccountInformationAsync())
                .Returns(fixture.FreezeFake<IAccountInformation>());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<IMegaApiClient>(),
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenHealthErrorShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IMegaApiClient>()
                .GetAccountInformationAsync())
                .Returns(null as IAccountInformation);

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<IMegaApiClient>(),
                cancellationToken);

            result.Should()
                .BeFailure().And
                .HaveError("Error while trying to retrieve information from account.");
        }
    }
}
