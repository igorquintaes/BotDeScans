using AutoFixture;
using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Services.Discord;
using BotDeScans.UnitTests.Extensions;
using FakeItEasy;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using System.Threading.Tasks;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Pings;
public class RoleTests : UnitTest
{
    private readonly RolePing ping;

    public RoleTests()
    {
        fixture.Freeze<PublishState>();
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
                    fixture.Freeze<PublishState>().Title.DiscordRoleId!.Value.ToString(),
                    cancellationToken))
                .Returns(Result.Ok(fixture.FreezeFake<IRole>()));
        }

        [Fact]
        public async Task ShouldReturnExpectedText()
        {
            var expectedText = $"<@&{titleRoleId.Value}>";

            var result = await ping.GetPingAsTextAsync(cancellationToken);
            result.Should().BeSuccess().And.HaveValue(expectedText);
        }

        [Fact]
        public async Task GivenErrorToGetDiscordRoleShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some message.";

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleFromGuildAsync(
                    fixture.Freeze<PublishState>().Title.DiscordRoleId!.Value.ToString(),
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await ping.GetPingAsTextAsync(cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }
    }
}
