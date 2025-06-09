using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
namespace BotDeScans.UnitTests.Specs.Models.Entities;

public class TitleValidatorTests : UnitTest
{
    public TitleValidatorTests() =>
        fixture.FreezeFake<RolesService>();

    [Theory]
    [InlineData(PingType.None, 0UL)]
    [InlineData(PingType.None, null)]
    [InlineData(PingType.Everyone, 0UL)]
    [InlineData(PingType.Everyone, null)]
    public async Task GivenValidDataForNonRequiredPingRoleShouldReturnSuccess(PingType pingType, ulong? roleId)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());
        var title = fixture.Build<Title>().With(x => x.DiscordRoleId, roleId).Create();
        var result = await fixture
              .Create<TitleValidator>()
              .TestValidateAsync(title, default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();

        A.CallTo(() => fixture
              .FreezeFake<RolesService>()
              .GetRoleAsync(A<string>.Ignored, cancellationToken))
              .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(PingType.None, 11UL)]
    [InlineData(PingType.Everyone, 1UL)]
    public async Task GivenValidDataForNonRequiredPingRoleButFilledShouldReturnSuccess(PingType pingType, ulong roleId)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());
        var title = fixture.Build<Title>().With(x => x.DiscordRoleId, roleId).Create();
        var result = await fixture
              .Create<TitleValidator>()
              .TestValidateAsync(title, default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();

        A.CallTo(() => fixture
              .FreezeFake<RolesService>()
              .GetRoleAsync(A<string>.Ignored, cancellationToken))
              .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(PingType.Role, 1UL)]
    [InlineData(PingType.Global, 1UL)]
    public async Task GivenValidDataForRequiredPingRoleShouldReturnSuccess(PingType pingType, ulong roleId)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());
        A.CallTo(() => fixture
              .FreezeFake<RolesService>()
              .GetRoleAsync(roleId.ToString(), cancellationToken))
              .Returns(Result.Ok(fixture.FreezeFake<IRole>()));

        var title = fixture.Build<Title>().With(x => x.DiscordRoleId, roleId).Create();
        var result = await fixture
              .Create<TitleValidator>()
              .TestValidateAsync(title, default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(PingType.Role, 0UL)]
    [InlineData(PingType.Role, null)]
    [InlineData(PingType.Global, 0UL)]
    [InlineData(PingType.Global, null)]
    public async Task GivenRequiredPingRoleShouldHaveDiscordRoleIdDefined(PingType pingType, ulong? roleId)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());

        var expectedErrorMessage =
              $"Não foi definida uma role para o Discord nesta obra, obrigatória para o ping de tipo {pingType}. " +
               "Defina, ou mude o tipo de ping para publicação no arquivo de configuração do Bot de Scans.";

        var title = fixture.Build<Title>().With(x => x.DiscordRoleId, roleId).Create();
        var result = await fixture
              .Create<TitleValidator>()
              .TestValidateAsync(title, default, cancellationToken);

        result.ShouldHaveValidationErrorFor(prop => prop.DiscordRoleId)
              .WithErrorMessage(expectedErrorMessage)
              .Only();
    }

    [Theory]
    [InlineData(PingType.Role, 1UL)]
    [InlineData(PingType.Global, 1UL)]
    public async Task GivenRequiredPingRoleAndErrorToGetDiscordRoleShouldReturnError(PingType pingType, ulong roleId)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());
        A.CallTo(() => fixture
              .FreezeFake<RolesService>()
              .GetRoleAsync(roleId.ToString(), cancellationToken))
              .Returns(Result.Fail(["err-1", "err-2"]));


        var title = fixture.Build<Title>().With(x => x.DiscordRoleId, roleId).Create();
        var result = await fixture
              .Create<TitleValidator>()
              .TestValidateAsync(title, default, cancellationToken);

        result.ShouldHaveValidationErrorFor(prop => prop.DiscordRoleId)
              .WithErrorMessage("err-1; err-2")
              .Only();
    }
}