using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Initializations.Factories;
using FluentAssertions.Execution;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.Factories;

public class SakuraMangasClientFactoryTests : UnitTest
{
    private readonly SakuraMangasClientFactory factory;

    public SakuraMangasClientFactoryTests()
    {
        fixture.FreezeFake<IConfiguration>();

        factory = fixture.Create<SakuraMangasClientFactory>();
    }

    public class Enabled : SakuraMangasClientFactoryTests
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
                    .Except([StepName.UploadSakuraMangas])
                    .Select(x => x.ToString()));

            factory.Enabled.Should().BeFalse();
        }
    }

    public class CreateAsync : SakuraMangasClientFactoryTests
    {
        [Fact]
        public async Task GivenExecutionShouldReturnExpectedResult()
        {
            var user = fixture.Create<string>();
            var pass = fixture.Create<string>();

            fixture.FreezeFakeConfiguration("SakuraMangas:User", user);
            fixture.FreezeFakeConfiguration("SakuraMangas:Pass", pass);

            var result = await factory.CreateAsync(cancellationToken);

            var httpClient = (HttpClient)typeof(SakuraMangasService)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(x => x.FieldType == typeof(HttpClient))
                .GetValue(result.Value)!;

            var userField = (string)typeof(SakuraMangasService)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(x => x.Name == "<user>P")
                .GetValue(result.Value)!;

            var passField = (string)typeof(SakuraMangasService)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(x => x.Name == "<pass>P")
                .GetValue(result.Value)!;

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            userField.Should().Be(user);
            passField.Should().Be(pass);
            httpClient.DefaultRequestHeaders.FirstOrDefault(x => x.Key == "User-Agent").Value.As<string?>()?
                      .Should().StartWith("BotDeScans");
            httpClient.DefaultRequestHeaders.FirstOrDefault(x => x.Key == "Accept").Value.As<string?>()?
                      .Should().Be("application/json");
        }
    }

    public class HealthCheckAsync : SakuraMangasClientFactoryTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<SakuraMangasService>()
                .PingCredentialsAsync(cancellationToken))
            .Returns(Result.Ok());

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<SakuraMangasService>(),
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<SakuraMangasService>()
                .PingCredentialsAsync(cancellationToken))
            .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<SakuraMangasService>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
