using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Initializations.FactoriesValidators;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.FactoriesValidators;

public class MangaDexClientFactoryValidatorTests : UnitTest
{
    public MangaDexClientFactoryValidatorTests()
    {
        fixture.FreezeFakeConfiguration("Mangadex:GroupId", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Mangadex:Username", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Mangadex:Password", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Mangadex:ClientId", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Mangadex:ClientSecret", fixture.Create<string>());
    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<MangaDexClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<MangaDexClientFactory>())
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("Mangadex:GroupId")]
    [InlineData("Mangadex:Username")]
    [InlineData("Mangadex:Password")]
    [InlineData("Mangadex:ClientId")]
    [InlineData("Mangadex:ClientSecret")]
    public void GivenMissingKeyShouldReturnError(string key)
    {
        fixture.FreezeFakeConfiguration(key, default(string));

        fixture.Create<MangaDexClientFactoryValidator>()
               .TestValidate(fixture.Freeze<MangaDexClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage($"'{key}' config value not found.")
               .Only();
    }
}
