using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Initializations;
using FluentResults;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations;

public class SetupClientsServiceValidatorTests : UnitTest
{
    private readonly string globalPing;

    public SetupClientsServiceValidatorTests()
    {
        globalPing = fixture.Create<string>();

        fixture.FreezeFake<RolesService>();
        fixture.FreezeFakeConfiguration("Discord:Token", fixture.Create<string>());
        fixture.FreezeFakeConfiguration("Discord:ReleaseChannel", fixture.Create<ulong>().ToString());
        fixture.FreezeFakeConfiguration("Discord:ServerId", fixture.Create<ulong>().ToString());
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Global.ToString());
        fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, globalPing);
    }

    [Fact]
    public async Task GivenValidDataShouldReturnSuccess()
    {
        var result = await fixture
            .Create<SetupDiscordServiceValidator>()
            .TestValidateAsync(fixture.Freeze<SetupDiscordService>(), default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(PingType.Everyone)]
    [InlineData(PingType.None)]
    [InlineData(PingType.Role)]
    public async Task GivenNotGlobalPingTypeShouldNotCheckGlobalRoleExistence(PingType pingType)
    {
        fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());

        var result = await fixture
            .Create<SetupDiscordServiceValidator>()
            .TestValidateAsync(fixture.Freeze<SetupDiscordService>(), default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();

        A.CallTo(() => fixture
            .FreezeFake<RolesService>()
            .GetRoleFromGuildAsync(A<string>.Ignored, cancellationToken))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData("Discord:Token")]
    [InlineData("Discord:ReleaseChannel")]
    [InlineData("Discord:ServerId")]
    [InlineData(Ping.PING_TYPE_KEY)]
    [InlineData(GlobalPing.GLOBAL_ROLE_KEY)]
    public async Task GivenMissingKeyShouldReturnError(string key)
    {
        fixture.FreezeFakeConfiguration(key, default(string));

        var result = await fixture
            .Create<SetupDiscordServiceValidator>()
            .TestValidateAsync(fixture.Freeze<SetupDiscordService>(), default, cancellationToken);

        result.ShouldHaveValidationErrorFor(service => service)
            .WithErrorMessage($"'{key}' config value not found.")
            .Only();
    }

    [Fact]
    public async Task GivenErrorToGetRoleShouldReturnValidarionError()
    {
        A.CallTo(() => fixture
            .FreezeFake<RolesService>()
            .GetRoleFromGuildAsync(globalPing, cancellationToken))
            .Returns(Result.Fail(["err-1", "err-2"]));

        var result = await fixture
            .Create<SetupDiscordServiceValidator>()
            .TestValidateAsync(fixture.Freeze<SetupDiscordService>(), default, cancellationToken);

        result.ShouldHaveValidationErrorFor(service => service)
            .WithErrorMessage("err-1; err-2")
            .Only();
    }
}
