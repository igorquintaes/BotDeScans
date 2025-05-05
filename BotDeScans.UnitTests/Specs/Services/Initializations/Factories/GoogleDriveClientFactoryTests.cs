using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.Factories;

public class GoogleDriveClientFactoryTests : UnitTest
{
    private readonly GoogleDriveClientFactory factory;

    public GoogleDriveClientFactoryTests()
    {
        fixture.FreezeFake<GoogleWrapper>();

        factory = fixture.Create<GoogleDriveClientFactory>();
    }

    public class Enabled : GoogleDriveClientFactoryTests
    {
        [Fact]
        public void ShouldReturnTrueResult() =>
            factory.Enabled.Should().BeTrue();
    }

    public class CreateAsync : GoogleDriveClientFactoryTests
    {
        private readonly string credentialsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config",
            GoogleDriveClientFactory.CREDENTIALS_FILE_NAME);

        // Fake key from https://github.com/googleapis/google-api-dotnet-client/blob/main/Src/Support/Google.Apis.Auth.Tests/OAuth2/GoogleCredentialTests.cs
        private const string fakeCredentials = @"{
""private_key_id"": ""PRIVATE_KEY_ID"",
""private_key"": ""-----BEGIN PRIVATE KEY-----
MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAJJM6HT4s6btOsfe
2x4zrzrwSUtmtR37XTTi0sPARTDF8uzmXy8UnE5RcVJzEH5T2Ssz/ylX4Sl/CI4L
no1l8j9GiHJb49LSRjWe4Yx936q0Xj9H0R1HTxvjUPqwAsTwy2fKBTog+q1frqc9
o8s2r6LYivUGDVbhuUzCaMJsf+x3AgMBAAECgYEAi0FTXsu/zRswAUGaViQiHjrL
uU65BSHXNVjV/2fLNEKnGWGqpli68z1IXY+S2nwbUak7rnGsq9/0F6jtsW+hZbLk
KXUOuuExpeC5Kd6ngWX/f2jqmhlUabiQijU9cVk7pMq8EHkRtvlosnMTUAEzempu
QUPwn1PZHhmJkBvZ4lECQQDCErrxl+e3BwUDcS0yVEEmCNSG6xdXs2878b8rzbe7
3Mmi6SuuOLi3PU92J+j+f/MOdtYrk13mEDdYmd5dhrt5AkEAwPvDEsDT/W4y4h5n
gv1awGBA5aLFE1JNWM/Gwn4D1cGpEDHKFREaBtxMDCASpHJuw8r7zUywpKhmBZcf
GS37bwJANdSAKfbafLfjuhqwUJ9yGpykZm/a36aTmerp/bpn1iHdg+RtCzwMcDb/
TWSwibbvsflgWmHbz657y4WSWhq+8QJAWrpCNN/ZCk2zuGDo80lfUBAwkoVat8G6
wWU1oZyS+vzIGef+hLb8kHsjeZPej9eIwZ39kcBbT54oELrCkRjwGwJAQ8V2A7lT
ZUp8AsbVqF6rbLiiUfJMo2btGclQu4DEVyS+ymFA65tXDLUuR9EDqJYdqHNZJ5B8
4Z5p2prkjWTLcA\u003d\u003d
-----END PRIVATE KEY-----"",
""client_email"": ""CLIENT_EMAIL"",
""client_id"": ""CLIENT_ID"",
""project_id"": ""PROJECT_ID"",
""type"": ""service_account""}";

        public CreateAsync()
        {
            if (System.IO.File.Exists(credentialsPath))
                System.IO.File.Delete(credentialsPath);

            using var textFile = System.IO.File.CreateText(credentialsPath);
            textFile.Write(fakeCredentials);
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

            await Verify(result.ValueOrDefault);
        }
    }

    public class HealthCheckAsync : GoogleDriveClientFactoryTests
    {
        public HealthCheckAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<DriveService>().Files)
                .Returns(fixture.FreezeFake<FilesResource>());

            A.CallTo(() => fixture
                .FreezeFake<FilesResource>().List())
                .Returns(fixture.FreezeFake<FilesResource.ListRequest>());

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(fixture.FreezeFake<FilesResource.ListRequest>(), cancellationToken))
                .Returns(Result.Ok<FileList>(default!));
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<DriveService>(),
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldQueryWithExpectedParameters()
        {
            await factory.HealthCheckAsync(
                fixture.FreezeFake<DriveService>(),
                cancellationToken);

            fixture.FreezeFake<FilesResource.ListRequest>().PageSize.Should().Be(1);
            fixture.FreezeFake<FilesResource.ListRequest>().Q.Should().Be("'root' in parents");
        }

        [Fact]
        public async Task GivenExecutionErrorShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error.";

            A.CallTo(() => fixture
                .FreezeFake<GoogleWrapper>()
                .ExecuteAsync(fixture.FreezeFake<FilesResource.ListRequest>(), cancellationToken))
                .Returns(Result.Fail<FileList>(ERROR_MESSAGE));

            var result = await factory.HealthCheckAsync(
                fixture.FreezeFake<DriveService>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
