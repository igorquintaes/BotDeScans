using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Initializations.FactoriesValidators;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.FactoriesValidators;

public class CapivaraTradutorClientFactoryValidatorTests : UnitTest
{
    public CapivaraTradutorClientFactoryValidatorTests()
    {
        fixture.FreezeFakeConfiguration("Capivara:GroupId", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Capivara:ApiKey", fixture.Create<string>());
    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<CapivaraTradutorClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<CapivaraTradutorClientFactory>())
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("Capivara:GroupId")]
    [InlineData("Capivara:ApiKey")]
    public void GivenMissingKeyShouldReturnError(string key)
    {
        fixture.FreezeFakeConfiguration(key, default(string));

        fixture.Create<CapivaraTradutorClientFactoryValidator>()
               .TestValidate(fixture.Freeze<CapivaraTradutorClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage($"'{key}' config value not found.")
               .Only();
    }
}
