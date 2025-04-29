using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Models;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Models;

public class TitleValidatorTests : UnitTest
{
    public TitleValidatorTests() =>
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Everyone.ToString());

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<TitleValidator>()
               .TestValidate(fixture.Create<Title>())
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GivenNonePingTypeShouldReturnError(string? pingType)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType);

        fixture.Create<TitleValidator>()
               .TestValidate(fixture.Create<Title>())
               .ShouldHaveValidationErrorFor(x => x)
               .WithErrorMessage("É necessário definir um tipo de ping no arquivo de configuração do Bot de Scans.");
    }

    [Fact]
    public void GivenInvalidPingTypeShouldReturnError()
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, "invalid");

        fixture.Create<TitleValidator>()
               .TestValidate(fixture.Create<Title>())
               .ShouldHaveValidationErrorFor(x => x)
               .WithErrorMessage("Valor inválido para o tipo de ping no arquivo de configuração do Bot de Scans.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GivenGlobalPingTypeShouldReturnErrorGlobalPingValueIsNotDefines(string? globalPingValue)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Global.ToString());
        fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, globalPingValue);

        fixture.Create<TitleValidator>()
               .TestValidate(fixture.Create<Title>())
               .ShouldHaveValidationErrorFor(x => x)
               .WithErrorMessage("É necessário definir um valor para ping global no arquivo de configuração do Bot de Scans.");
    }

    [Theory]
    [InlineData(PingType.Role, 0UL)]
    [InlineData(PingType.Role, null)]
    [InlineData(PingType.Global, 0UL)]
    [InlineData(PingType.Global, null)]
    public void GivenSomePingTypesShouldHaveDiscordRoleIdDefined(PingType pingType, ulong? roleId)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());
        fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, "some-role");

        fixture.Create<TitleValidator>()
               .TestValidate(fixture.Create<Title>() with { DiscordRoleId = roleId })
               .ShouldHaveValidationErrorFor(x => x.DiscordRoleId)
               .WithErrorMessage(
                     $"Não foi definida uma role para o Discord nesta obra, obrigatória para o ping de tipo {pingType}. " +
                     $"Defina, ou mude o tipo de ping para publicação no arquivo de configuração do Bot de Scans.");
    }
}