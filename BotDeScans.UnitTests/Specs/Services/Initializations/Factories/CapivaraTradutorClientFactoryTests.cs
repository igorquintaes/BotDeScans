using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.CapivaraTradutor;
using BotDeScans.App.Services.CapivaraTradutor.InternalServices;
using BotDeScans.App.Services.Initializations.Factories;
using FluentAssertions.Execution;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.Factories;

public class CapivaraTradutorClientFactoryTests : UnitTest
{
    private readonly CapivaraTradutorClientFactory factory;

    public CapivaraTradutorClientFactoryTests()
    {
        fixture.FreezeFake<IConfiguration>();
        fixture.FreezeFake<CapivaraTradutorUploadService>();

        factory = fixture.Create<CapivaraTradutorClientFactory>();
    }

    public class Enabled : CapivaraTradutorClientFactoryTests
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
                    .Except([StepName.UploadCapivaraTradutor])
                    .Select(x => x.ToString()));

            factory.Enabled.Should().BeFalse();
        }
    }

    public class CreateAsync : CapivaraTradutorClientFactoryTests
    {
        [Fact]
        public async Task GivenExecutionShouldReturnExpectedResult()
        {
            var groupId = fixture.Create<string>();
            var apiKey = fixture.Create<string>();

            fixture.FreezeFakeConfiguration("Capivara:GroupId", groupId);
            fixture.FreezeFakeConfiguration("Capivara:ApiKey", apiKey);

            var result = await factory.CreateAsync(cancellationToken);

            var httpClient = (HttpClient)typeof(CapivaraTradutorService)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(x => x.FieldType == typeof(HttpClient))
                .GetValue(result.Value)!;

            using var _ = new AssertionScope();
            result.Should().BeSuccess();

            httpClient.DefaultRequestHeaders.FirstOrDefault(x => x.Key == "GroupId")
                      .Value.As<string?>()?.Should().Be(groupId);
            httpClient.DefaultRequestHeaders.FirstOrDefault(x => x.Key == "User-Agent")
                      .Value.As<string?>()?.Should().StartWith("BotDeScans");
            httpClient.DefaultRequestHeaders.FirstOrDefault(x => x.Key == "Accept")
                      .Value.As<string?>()?.Should().Be("application/json");

            httpClient.DefaultRequestHeaders.Authorization?.Scheme.Should().Be("Bearer");
            httpClient.DefaultRequestHeaders.Authorization?.Parameter.Should().Be(apiKey);
        }
    }

    public class HealthCheckAsync : CapivaraTradutorClientFactoryTests
    {
        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorService>()
                .PingCredentialsAsync(cancellationToken))
            .Returns(Result.Ok());

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<CapivaraTradutorService>(),
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<CapivaraTradutorService>()
                .PingCredentialsAsync(cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<CapivaraTradutorService>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
