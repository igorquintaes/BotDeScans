using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Pings;

public class RoleTests : UnitTest
{
    private readonly RolePing ping;

    public RoleTests()
    {
        fixture.Freeze<State>();
        fixture.FreezeFake<RolesService>();
        fixture.FreezeFake<IConfiguration>();

        ping = fixture.Create<RolePing>();
    }

    public class IsApplicable : RoleTests
    {
        [Fact]
        public void GivenExpectedPingTypeShouldReturnTrue()
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Role.ToString());

            ping.IsApplicable.Should().BeTrue();
        }

        [Theory]
        [InlineData(PingType.Everyone)]
        [InlineData(PingType.Global)]
        [InlineData(PingType.None)]
        [InlineData((PingType)999)]
        public void GivenUnexpectedPingTypeShouldReturnFalse(PingType pingType)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());

            ping.IsApplicable.Should().BeFalse();
        }
    }

    public class GetPingAsTextAsync : RoleTests
    {
        private readonly Snowflake titleRoleId;

        public GetPingAsTextAsync()
        {
            titleRoleId = new(fixture.Create<ulong>());

            A.CallTo(() => fixture
                .FreezeFake<IRole>().ID)
                .Returns(titleRoleId);

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleFromGuildAsync(
                    fixture.Freeze<State>().Title.DiscordRoleId!.Value.ToString(),
                    cancellationToken))
                .Returns(Result.Ok(fixture.FreezeFake<IRole>()));
        }

        [Fact]
        public async Task ShouldReturnExpectedText()
        {
            var expectedText = $"<@&{titleRoleId.Value}>";

            var result = await ping.GetPingAsTextAsync(cancellationToken);
            result.Should().Be(expectedText);
        }
    }
}
