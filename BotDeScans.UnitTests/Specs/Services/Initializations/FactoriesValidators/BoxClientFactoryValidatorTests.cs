using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Initializations.FactoriesValidators;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.FactoriesValidators;

public class BoxClientFactoryValidatorTests : UnitTest
{
    public BoxClientFactoryValidatorTests()
    {
        fixture.FreezeFakeConfiguration("Box:ClientId", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Box:ClientSecret", fixture.Create<string>());
    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<BoxClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<BoxClientFactory>())
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("Box:ClientId")]
    [InlineData("Box:ClientSecret")]
    public void GivenMissingKeyShouldReturnError(string key)
    {
        fixture.FreezeFakeConfiguration(key, default(string));

        fixture.Create<BoxClientFactoryValidator>()
               .TestValidate(fixture.Freeze<BoxClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage($"'{key}' config value not found.")
               .Only();
    }
}
