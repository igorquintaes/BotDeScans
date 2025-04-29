using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
namespace BotDeScans.UnitTests.Specs.Features.Publish.Pings;

public class GlobalTests : UnitTest
{
    private readonly GlobalPing ping;

    public GlobalTests()
    {
        fixture.Freeze<PublishState>();
        fixture.FreezeFake<RolesService>();
        fixture.FreezeFake<IConfiguration>();

        ping = fixture.Create<GlobalPing>();
    }

    public class IsApplicable : GlobalTests
    {
        [Fact]
        public void GivenExpectedPingTypeShouldReturnTrue()
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Global.ToString());

            ping.IsApplicable.Should().BeTrue();
        }

        [Theory]
        [InlineData(PingType.Everyone)]
        [InlineData(PingType.None)]
        [InlineData(PingType.Role)]
        [InlineData((PingType)999)]
        public void GivenUnexpectedPingTypeShouldReturnFalse(PingType pingType)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());

            ping.IsApplicable.Should().BeFalse();
        }
    }

    public class GetPingAsTextAsync : GlobalTests
    {
        private readonly Snowflake globalRoleId;
        private readonly Snowflake titleRoleId;

        public GetPingAsTextAsync()
        {
            var globalRoleName = fixture.Create<string>();
            globalRoleId = new(fixture.Create<ulong>());
            titleRoleId = new(fixture.Create<ulong>());

            var globalRole = A.Fake<IRole>();
            var titleRole = A.Fake<IRole>();
            A.CallTo(() => globalRole.ID).Returns(globalRoleId);
            A.CallTo(() => titleRole.ID).Returns(titleRoleId);

            fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, globalRoleName);

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleFromGuildAsync(globalRoleName, cancellationToken))
                .Returns(Result.Ok(globalRole));

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleFromGuildAsync(
                    fixture.Freeze<PublishState>().Title.DiscordRoleId!.Value.ToString(),
                    cancellationToken))
                .Returns(Result.Ok(titleRole));
        }

        [Fact]
        public async Task ShouldReturnExpectedText()
        {
            var expectedText = $"<@&{globalRoleId.Value}>, <@&{titleRoleId.Value}>";

            var result = await ping.GetPingAsTextAsync(cancellationToken);
            result.Should().Be(expectedText);
        }
    }
}
