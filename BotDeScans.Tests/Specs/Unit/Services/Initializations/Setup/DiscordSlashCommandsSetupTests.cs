using BotDeScans.App.Services.Initializations.Setups;
using BotDeScans.App.Services.Wrappers;
using Remora.Rest.Core;
using Remora.Results;

namespace BotDeScans.Tests.Specs.Services.Initializations.Setup;

public class DiscordSlashCommandsSetupTests : UnitTest
{
    private readonly DiscordSlashCommandsSetup setup;

    private readonly ulong serverId;

    public DiscordSlashCommandsSetupTests()
    {
        serverId = fixture.Create<ulong>();
        fixture.FreezeFake<SlashServiceWrapper>();
        fixture.FreezeFakeConfiguration("Discord:ServerId", serverId.ToString());

        setup = fixture.Create<DiscordSlashCommandsSetup>();

        var remoraResult = A.Fake<IResult>();
        A.CallTo(() => remoraResult.IsSuccess).Returns(true);

        A.CallTo(() => fixture
            .FreezeFake<SlashServiceWrapper>()
            .UpdateSlashCommandsAsync(new Snowflake(serverId, 0), default, cancellationToken))
            .Returns(remoraResult);

    }

    public class ExecuteAsync : DiscordSlashCommandsSetupTests
    {
        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccessResult()
        {
            var result = await setup.ExecuteAsync(cancellationToken);

            result.Should().BeSuccess().And.HaveReason("Discord slash commands setup completed.");
        }

        [Fact]
        public async Task GivenErrorToUpdateSlashCommandShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            var remoraResult = A.Fake<IResult>();
            var remoraError = A.Fake<IResultError>();

            A.CallTo(() => remoraResult.IsSuccess).Returns(false);
            A.CallTo(() => remoraResult.Error).Returns(remoraError);
            A.CallTo(() => remoraError.Message).Returns(ERROR_MESSAGE);

            A.CallTo(() => fixture
                .FreezeFake<SlashServiceWrapper>()
                .UpdateSlashCommandsAsync(new Snowflake(serverId, 0), default, cancellationToken))
                .Returns(remoraResult);

            var result = await setup.ExecuteAsync(cancellationToken);

            result.Should().BeFailure().And
                .HaveError("Error in Discord communication.").And
                .HaveError(ERROR_MESSAGE);
        }
    }
}
