using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Initializations.FactoriesValidators;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.FactoriesValidators;

public class BoxClientFactoryValidatorTests : UnitTest
{
    private static readonly string credentialPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config",
            BoxClientFactory.CREDENTIALS_FILE_NAME);

    public BoxClientFactoryValidatorTests()
    {
        if (File.Exists(credentialPath) is false)
            File.Create(credentialPath).Dispose();

    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<BoxClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<BoxClientFactory>())
               .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GivenMissingCredentialFileShouldReturnError()
    {
        File.Delete(credentialPath);

        fixture.Create<BoxClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<BoxClientFactory>())
               .ShouldHaveValidationErrorFor(x => x)
               .WithErrorMessage($"Unable to find IBoxClient file: {credentialPath}")
               .Only();
    }
}
