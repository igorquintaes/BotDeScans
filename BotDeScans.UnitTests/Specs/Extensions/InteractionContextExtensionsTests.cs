using BotDeScans.App.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class InteractionContextExtensionsTests : UnitTest
{
    public class GetUserAvatarUrl : InteractionContextExtensionsTests
    {
        [Fact]
        public void GivenUserWithAvatarShouldReturnExpectedUrl()
        {
            var userId = new Snowflake(fixture.Create<ulong>());
            var avatarHash = fixture.Create<string>();
            var expectedUrl = $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png";

            var user = fixture.FreezeFake<IUser>();
            A.CallTo(() => user.ID).Returns(userId);
            A.CallTo(() => user.Avatar).Returns(fixture.FreezeFake<IImageHash>());
            A.CallTo(() => user.Avatar!.Value!).Returns(avatarHash);

            var member = fixture.FreezeFake<IGuildMember>();
            A.CallTo(() => member.User).Returns(new Optional<IUser>(user));

            var interaction = fixture.FreezeFake<IInteraction>();
            A.CallTo(() => interaction.Member).Returns(new Optional<IGuildMember>(member));

            var context = new InteractionContext(interaction);

            var result = context.GetUserAvatarUrl();

            result.HasValue.Should().BeTrue();
            result.Value.Should().Be(expectedUrl);
        }

        [Fact]
        public void GivenUserWithoutAvatarShouldReturnEmptyOptional()
        {
            var userId = new Snowflake(fixture.Create<ulong>());

            var user = fixture.FreezeFake<IUser>();
            A.CallTo(() => user.ID).Returns(userId);
            A.CallTo(() => user.Avatar).Returns(null);

            var member = fixture.FreezeFake<IGuildMember>();
            A.CallTo(() => member.User).Returns(new Optional<IUser>(user));

            var interaction = fixture.FreezeFake<IInteraction>();
            A.CallTo(() => interaction.Member).Returns(new Optional<IGuildMember>(member));

            var context = new InteractionContext(interaction);

            var result = context.GetUserAvatarUrl();

            result.HasValue.Should().BeFalse();
        }
    }

    public class GetUserName : InteractionContextExtensionsTests
    {
        [Fact]
        public void GivenUserWithUsernameShouldReturnUsername()
        {
            var expectedUsername = fixture.Create<string>();

            var user = fixture.FreezeFake<IUser>();
            A.CallTo(() => user.Username).Returns(expectedUsername);

            var member = fixture.FreezeFake<IGuildMember>();
            A.CallTo(() => member.User).Returns(new Optional<IUser>(user));

            var interaction = fixture.FreezeFake<IInteraction>();
            A.CallTo(() => interaction.Member).Returns(new Optional<IGuildMember>(member));

            var context = new InteractionContext(interaction);

            var result = context.GetUserName();

            result.Should().Be(expectedUsername);
        }
    }
}