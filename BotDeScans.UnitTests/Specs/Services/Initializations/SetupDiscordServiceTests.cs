using BotDeScans.App.Services.Initializations;
using BotDeScans.App.Services.Wrappers;
using Remora.Rest.Core;
using Remora.Results;

namespace BotDeScans.UnitTests.Specs.Services.Initializations;

public class SetupDiscordServiceTests : UnitTest
{
    private readonly SetupDiscordService service;

    private readonly ulong serverId;

    public SetupDiscordServiceTests()
    {
        serverId = fixture.Create<ulong>();
        fixture.FreezeFake<SlashServiceWrapper>();
        fixture.FreezeFakeConfiguration("Discord:ServerId", serverId.ToString());

        service = fixture.Create<SetupDiscordService>();

        var errorResult = A.Fake<IResult>();
        A.CallTo(() => errorResult.IsSuccess).Returns(true);

        A.CallTo(() => fixture
            .FreezeFake<SlashServiceWrapper>()
            .UpdateSlashCommandsAsync(new Snowflake(serverId, 0), default, cancellationToken))
            .Returns(errorResult);

    }

    public class SetupAsync : SetupDiscordServiceTests
    {
        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccessResult()
        {
            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenErrorToUpdateSlashCommandShouldReturnFailResult()
        {
            var errorResult = A.Fake<IResult>();
            A.CallTo(() => errorResult.IsSuccess).Returns(false);

            A.CallTo(() => fixture
                .FreezeFake<SlashServiceWrapper>()
                .UpdateSlashCommandsAsync(new Snowflake(serverId, 0), default, cancellationToken))
                .Returns(errorResult);

            var result = await service.SetupAsync(cancellationToken);

            result.Should().BeFailure().And.HaveError("Failed to update Discord slash commands.");
        }
    }
}
