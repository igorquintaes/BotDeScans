using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Pings;

public class GlobalTests : UnitTest
{
    private readonly GlobalPing ping;

    public GlobalTests()
    {
        fixture.Freeze<State>();
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
                .GetRoleAsync(globalRoleName, cancellationToken))
                .Returns(Result.Ok(globalRole));

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(
                    fixture.Freeze<State>().Title.DiscordRoleId!.Value.ToString(),
                    cancellationToken))
                .Returns(Result.Ok(titleRole));
        }

        [Fact]
        public async Task ShouldReturnExpectedText()
        {
            var expectedText = $"<@&{globalRoleId.Value}>, <@&{titleRoleId.Value}>";

            var result = await ping.GetPingAsTextAsync(null!, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(expectedText);
        }

        [Fact]
        public async Task ShouldReturnFailResultWhenGetGlobalRole()
        {
            const string ERROR_MESSAGE = "some error.";
            var titleRole = A.Fake<IRole>();

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(A<string>._, cancellationToken))
                .ReturnsNextFromSequence(
                    Result.Fail(ERROR_MESSAGE),
                    Result.Ok(titleRole));

            var result = await ping.GetPingAsTextAsync(null!, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task ShouldReturnFailResultWhenGetTitleRole()
        {
            const string ERROR_MESSAGE = "some error.";

            var globalRole = A.Fake<IRole>();
            A.CallTo(() => globalRole.ID).Returns(globalRoleId);

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(A<string>._, cancellationToken))
                .ReturnsNextFromSequence(
                    Result.Ok(globalRole),
                    Result.Fail(ERROR_MESSAGE));

            var result = await ping.GetPingAsTextAsync(null!, cancellationToken);
            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task ShouldMergeResultReasonsIfSuccess()
        {
            const string REASON_1 = "1";
            const string REASON_2 = "2";

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(A<string>._, cancellationToken))
                .ReturnsNextFromSequence(
                    Result.Ok(A.Fake<IRole>()).WithReason(new Success(REASON_1)),
                    Result.Ok(A.Fake<IRole>()).WithReason(new Success(REASON_2)));

            var result = await ping.GetPingAsTextAsync(null!, cancellationToken);

            result.Should().BeSuccess()
                  .And.HaveReason(REASON_1)
                  .And.HaveReason(REASON_2)
                  .Which.Reasons.Should().HaveCount(2);
        }

        [Fact]
        public async Task ShouldMergeResultReasonsIfFail()
        {
            const string REASON_1 = "1";
            const string REASON_2 = "2";

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(A<string>._, cancellationToken))
                .ReturnsNextFromSequence(
                    Result.Ok(A.Fake<IRole>()).WithReason(new Success(REASON_1)),
                    Result.Ok(A.Fake<IRole>()).WithReason(new Error(REASON_2)));

            var result = await ping.GetPingAsTextAsync(null!, cancellationToken);

            result.Should().BeFailure()
                  .And.HaveReason(REASON_1)
                  .And.HaveReason(REASON_2)
                  .Which.Reasons.Should().HaveCount(2);
        }
    }
}
