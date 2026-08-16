using BotDeScans.App.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Extensions;

public abstract class InteractionContextExtensionsTests : UnitTest
{
    public class GetAuthor : InteractionContextExtensionsTests
    {
        [Fact]
        public void GivenUserWithAvatarShouldReturnExpectedUrl()
        {
            var userId = new Snowflake(fixture.Create<ulong>());
            var avatarHash = fixture.Create<string>();
            var expectedUrl = $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png";

            SetupUserAvatarUrl(userId, avatarHash);

            var context = new InteractionContext(fixture
                .FreezeFake<IInteraction>())
                .GetAuthor();

            context.IconUrl.Value.Should().Be(expectedUrl);
        }

        [Fact]
        public void GivenUserWithoutAvatarShouldReturnEmptyOptional()
        {
            SetupUserAvatarUrl(new(), null);

            var context = new InteractionContext(fixture
                .FreezeFake<IInteraction>())
                .GetAuthor();

            context.IconUrl.HasValue.Should().BeFalse();
        }
    }

    public class GetUserName : InteractionContextExtensionsTests
    {
        [Fact]
        public void GivenUserWithUsernameShouldReturnUsername()
        {
            var userName = fixture.Create<string>();
            SetupUserName(userName);

            var context = new InteractionContext(fixture
                .FreezeFake<IInteraction>())
                .GetUserName();

            context.Should().Be(userName);
        }
    }

    private void SetupUserName(string userName)
    {
        var user = fixture.FreezeFake<IUser>();
        A.CallTo(() => user.Username).Returns(userName);

        var member = fixture.FreezeFake<IGuildMember>();
        A.CallTo(() => member.User).Returns(new Optional<IUser>(user));

        var interaction = fixture.FreezeFake<IInteraction>();
        A.CallTo(() => interaction.Member).Returns(new Optional<IGuildMember>(member));
    }

    private string SetupUserAvatarUrl(Snowflake userId, string? avatarHash)
    {
        var user = fixture.FreezeFake<IUser>();
        A.CallTo(() => user.ID).Returns(userId);
        A.CallTo(() => user.Avatar).Returns(fixture.FreezeFake<IImageHash>());
        A.CallTo(() => user.Avatar!.Value).Returns(avatarHash!);

        var member = fixture.FreezeFake<IGuildMember>();
        A.CallTo(() => member.User).Returns(new Optional<IUser>(user));

        var interaction = fixture.FreezeFake<IInteraction>();
        A.CallTo(() => interaction.Member).Returns(new Optional<IGuildMember>(member));

        return $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png";
    }
}