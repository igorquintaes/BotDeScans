using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Initializations.FactoriesValidators;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.FactoriesValidators;

public class SakuraMangasClientFactoryValidatorTests : UnitTest
{
    public SakuraMangasClientFactoryValidatorTests()
    {
        fixture.FreezeFakeConfiguration("SakuraMangas:User", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("SakuraMangas:Pass", fixture.Create<string>());
    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<SakuraMangasClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<SakuraMangasClientFactory>())
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("SakuraMangas:User")]
    [InlineData("SakuraMangas:Pass")]
    public void GivenMissingKeyShouldReturnError(string key)
    {
        fixture.FreezeFakeConfiguration(key, default(string));

        fixture.Create<SakuraMangasClientFactoryValidator>()
               .TestValidate(fixture.Freeze<SakuraMangasClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage($"'{key}' config value not found.")
               .Only();
    }
}