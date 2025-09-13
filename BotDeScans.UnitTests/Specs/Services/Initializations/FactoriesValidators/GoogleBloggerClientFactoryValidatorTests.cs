using BotDeScans.App.Services;
using BotDeScans.App.Services.Initializations.Factories;
using BotDeScans.App.Services.Initializations.FactoriesValidators;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.FactoriesValidators;

public class GoogleBloggerClientFactoryValidatorTests : UnitTest
{
    private static readonly string credentialPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config",
            GoogleBloggerClientFactory.CREDENTIALS_FILE_NAME);

    private static readonly string templatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "config",
            GoogleBloggerService.TEMPLATE_FILE_NAME);

    public GoogleBloggerClientFactoryValidatorTests()
    {
        fixture.FreezeFakeConfiguration("Blogger:Id", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Blogger:Url", "http://www.escoladescans.com");
        fixture.FreezeFakeConfiguration("Blogger:Cover:Width", "1");
        fixture.FreezeFakeConfiguration("Blogger:Cover:Height", "1");

        if (File.Exists(credentialPath) is false)
            File.Create(credentialPath).Dispose();

        if (File.Exists(templatePath) is false)
            File.Create(templatePath).Dispose();

    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<GoogleBloggerClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<GoogleBloggerClientFactory>())
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("Blogger:Id")]
    [InlineData("Blogger:Url")]
    [InlineData("Blogger:Cover:Width")]
    [InlineData("Blogger:Cover:Height")]
    public void GivenMissingKeyShouldReturnError(string key)
    {
        fixture.FreezeFakeConfiguration(key, default(string));

        fixture.Create<GoogleBloggerClientFactoryValidator>()
               .TestValidate(fixture.Freeze<GoogleBloggerClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage($"'{key}' config value not found.")
               .Only();
    }

    [Fact]
    public void GivenMissingCredentialFileShouldReturnError()
    {
        File.Delete(credentialPath);

        fixture.Create<GoogleBloggerClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<GoogleBloggerClientFactory>())
               .ShouldHaveValidationErrorFor(x => x)
               .WithErrorMessage($"Unable to find BloggerService file: {credentialPath}")
               .Only();
    }

    [Fact]
    public void GivenMissingTemplateFileShouldReturnError()
    {
        File.Delete(templatePath);

        fixture.Create<GoogleBloggerClientFactoryValidator>()
               .TestValidate(fixture.FreezeFake<GoogleBloggerClientFactory>())
               .ShouldHaveValidationErrorFor(x => x)
               .WithErrorMessage($"Unable to find BloggerService file: {templatePath}")
               .Only();
    }

    [Fact]
    public void GivenInvalidBloggerUrlShouldReturnError()
    {
        fixture.FreezeFakeConfiguration("Blogger:Url", fixture.Create<string>());

        fixture.Create<GoogleBloggerClientFactoryValidator>()
               .TestValidate(fixture.Freeze<GoogleBloggerClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage("Não foi possível identificar o link do Blogger como válido.")
               .Only();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public void GivenInvalidBloggerCoverWidthValueShouldReturnError(string width)
    {
        fixture.FreezeFakeConfiguration("Blogger:Cover:Width", width);

        fixture.Create<GoogleBloggerClientFactoryValidator>()
               .TestValidate(fixture.Freeze<GoogleBloggerClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage("A capa precisa ter um número válido de largura e superior a 0.")
               .Only();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public void GivenInvalidBloggerCoverHeightValueShouldReturnError(string height)
    {
        fixture.FreezeFakeConfiguration("Blogger:Cover:Height", height);

        fixture.Create<GoogleBloggerClientFactoryValidator>()
               .TestValidate(fixture.Freeze<GoogleBloggerClientFactory>())
               .ShouldHaveValidationErrorFor(service => service)
               .WithErrorMessage("A capa precisa ter um número válido de altura e superior a 0.")
               .Only();
    }
}
