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
        [MemberData(nameof(GetInvalidPingTypes))]
        public void GivenUnexpectedPingTypeShouldReturnFalse(PingType pingType)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());

            ping.IsApplicable.Should().BeFalse();
        }

        public static TheoryData<PingType> GetInvalidPingTypes() =>
            [.. Enum.GetValues<PingType>().Where(p => p != PingType.Role)];
    }

    public class GetPingAsTextAsync : RoleTests
    {
        private readonly Snowflake titleRoleId;

        public GetPingAsTextAsync()
        {
            titleRoleId = new(fixture.Create<ulong>());

            var state = fixture.Create<State>();
            fixture.Inject(state);

            A.CallTo(() => fixture
                .FreezeFake<IRole>().ID)
                .Returns(titleRoleId);

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(
                    fixture.Freeze<State>().Title.DiscordRoleId!.Value.ToString(),
                    cancellationToken))
                .Returns(Result.Ok(fixture.FreezeFake<IRole>()));
        }

        [Fact]
        public async Task ShouldReturnExpectedText()
        {
            var expectedText = $"<@&{titleRoleId.Value}>";

            var result = await ping.GetPingAsTextAsync(fixture.Freeze<State>(), cancellationToken);
            result.Should().BeSuccess().And.HaveValue(expectedText);
        }

        [Fact]
        public async Task ShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(
                    fixture.Freeze<State>().Title.DiscordRoleId!.Value.ToString(),
                    cancellationToken))
                .Returns(Result.Fail(ERROR_MESSAGE));

            var result = await ping.GetPingAsTextAsync(fixture.Freeze<State>(), cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenGetRoleAsyncMultipleReasonsShoultKeepThem()
        {
            const string SUCCESS_MESSAGE = "some success";
            const string ERROR_MESSAGE = "some error";

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(
                    fixture.Freeze<State>().Title.DiscordRoleId!.Value.ToString(),
                    cancellationToken))
                .Returns(new Result()
                    .WithSuccess(SUCCESS_MESSAGE)
                    .WithError(ERROR_MESSAGE));

            var result = await ping.GetPingAsTextAsync(fixture.Freeze<State>(), cancellationToken);

            result.Should().HaveError(ERROR_MESSAGE);
            result.Should().HaveReason(SUCCESS_MESSAGE);
            result.Reasons.Should().HaveCount(2);
            result.Reasons.Single(r => r.Message == SUCCESS_MESSAGE).Should().BeOfType<Success>();
        }
    }
}
