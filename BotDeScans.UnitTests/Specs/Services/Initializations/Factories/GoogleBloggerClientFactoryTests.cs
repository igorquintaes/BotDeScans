using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;
using Microsoft.Extensions.Configuration;
using System.Reflection;
namespace BotDeScans.UnitTests.Specs.Services.Initializations.Factories;

public class GoogleBloggerClientFactoryTests : UnitTest
{
    private readonly GoogleBloggerClientFactory factory;

    public GoogleBloggerClientFactoryTests()
    {
        factory = fixture.FreezeFake<GoogleBloggerClientFactory>(options => options
                         .CallsBaseMethods());

        typeof(GoogleBloggerClientFactory)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(x => x.FieldType == typeof(IConfiguration))
            .SetValue(factory, fixture.FreezeFake<IConfiguration>());

        typeof(GoogleBloggerClientFactory)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(x => x.FieldType == typeof(GoogleWrapper))
            .SetValue(factory, fixture.FreezeFake<GoogleWrapper>());

        factory = fixture.Create<GoogleBloggerClientFactory>();
    }

    public class Enabled : GoogleBloggerClientFactoryTests
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
                    .Except([StepName.PublishBlogspot])
                    .Select(x => x.ToString()));

            factory.Enabled.Should().BeFalse();
        }
    }

    public class CreateAsync : GoogleBloggerClientFactoryTests
    {
        private readonly string credentialsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config",
            GoogleBloggerClientFactory.CREDENTIALS_FILE_NAME);

        // Fake key from https://github.com/googleapis/google-api-dotnet-client/blob/main/Src/Support/Google.Apis.Auth.Tests/OAuth2/GoogleCredentialTests.cs
        private const string fakeCredentials = @"{
""client_id"": ""CLIENT_ID"",
""client_secret"": ""CLIENT_SECRET"",
""refresh_token"": ""REFRESH_TOKEN"",
""project_id"": ""PROJECT_ID"",
""quota_project_id"": ""QUOTA_PROJECT"",
""type"": ""authorized_user""}";

        public CreateAsync()
        {
            if (File.Exists(credentialsPath))
                File.Delete(credentialsPath);

            using var textFile = File.CreateText(credentialsPath);
            textFile.Write(fakeCredentials);

            A.CallTo(factory)
                .Where(call => call.Method.Name == "GetUserCredential")
                .WithReturnType<Task<UserCredential>>()
                .Returns(Task.FromResult(fixture.FreezeFake<UserCredential>()));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await factory.CreateAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldCreateExpectedObject()
        {
            var result = await factory.CreateAsync(cancellationToken);

            result.ValueOrDefault.Should().NotBeNull();
        }

        [Fact]
        public async Task GivenMissingConfigFileShouldReturnFailResult()
        {
            File.Delete(credentialsPath);

            var result = await factory.CreateAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError($"Unable to find BloggerService file: {credentialsPath}");
        }
    }

    public class HealthCheckAsync : GoogleBloggerClientFactoryTests
    {
        public HealthCheckAsync()
        {
            var bloggerId = fixture.Create<string>();
            fixture.FreezeFakeConfiguration("Blogger:Id", bloggerId);

            A.CallTo(() => fixture
                .FreezeFake<BloggerService>().Posts)
                .Returns(fixture.FreezeFake<PostsResource>());

            A.CallTo(() => fixture
                .FreezeFake<PostsResource>().List(bloggerId))
                .Returns(fixture.FreezeFake<PostsResource.ListRequest>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(fixture.FreezeFake<PostsResource.ListRequest>(), cancellationToken))
                .Returns(Result.Ok<PostList>(default!));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<BloggerService>(),
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldQueryWithExpectedParameters()
        {
            await factory.HealthCheckAsync(
                fixture.FreezeFake<BloggerService>(),
                cancellationToken);

            fixture.FreezeFake<PostsResource.ListRequest>().MaxResults.Should().Be(1);
        }

        [Fact]
        public async Task GivenExecutionErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(fixture.FreezeFake<PostsResource.ListRequest>(), cancellationToken))
                .Returns(Result.Fail<PostList>(ERROR_MESSAGE));

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<BloggerService>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
