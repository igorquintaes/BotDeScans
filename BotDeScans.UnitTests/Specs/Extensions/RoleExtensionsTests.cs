using BotDeScans.App.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Extensions;

public class RoleExtensionsTests : UnitTest
{
    public class ToDiscordString : RoleExtensionsTests
    {
        [Fact]
        public void GivenRoleShouldOutputExpectedResult()
        {
            var roleId = fixture.Create<Snowflake>();
            var role = fixture.FreezeFake<IRole>();
            A.CallTo(() => fixture.FreezeFake<IRole>().ID).Returns(roleId);

            var result = role.ToDiscordString();

            result.Should().Be($"<@&{roleId.Value}>");
        }
    }
}
