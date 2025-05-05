using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Initializations.FactoriesValidators;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.FactoriesValidators;

public class GoogleDriveClientFactoryValidatorTests : UnitTest
{
    private static readonly string credentialPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config",
            GoogleDriveClientFactory.CREDENTIALS_FILE_NAME);

    public GoogleDriveClientFactoryValidatorTests()
    {
        if (File.Exists(credentialPath) is false)
            File.Create(credentialPath).Dispose();
    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<GoogleDriveClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<GoogleDriveClientFactory>())
               .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GivenMissingCredentialFileShouldReturnError()
    {
        File.Delete(credentialPath);

        fixture.Create<GoogleDriveClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<GoogleDriveClientFactory>())
               .ShouldHaveValidationErrorFor(x => x)
               .WithErrorMessage($"Unable to find DriveService file: {credentialPath}")
               .Only();
    }
}
