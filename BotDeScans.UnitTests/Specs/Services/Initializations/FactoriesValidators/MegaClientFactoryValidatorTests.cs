using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Initializations.FactoriesValidators;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.FactoriesValidators;

public class MegaClientFactoryValidatorTests : UnitTest
{
    public MegaClientFactoryValidatorTests()
    {
        fixture.FreezeFakeConfiguration("Mega:User", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Mega:Pass", fixture.Create<string>());
    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<MegaClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<MegaClientFactory>())
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("Mega:User")]
    [InlineData("Mega:Pass")]
    public void GivenMissingKeyShouldReturnError(string key)
    {
        fixture.FreezeFakeConfiguration(key, default(string));

        fixture.Create<MegaClientFactoryValidator>()
               .TestValidate(fixture.Freeze<MegaClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage($"'{key}' config value not found.")
               .Only();
    }
}
