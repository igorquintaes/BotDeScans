using Bogus;
using FakeItEasy;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Builders
{
    public class InteractionContextBuilder
    {
        private Faker faker = new();
        private Snowflake id;
        private Snowflake guildID;
        private IUser user;
        private IGuildMember member;
        private string token;

        public InteractionContextBuilder()
        {
            id = new Snowflake(faker.Random.ULong());
            guildID = new Snowflake(faker.Random.ULong());
            token = faker.Random.String();
            user = A.Fake<IUser>();
            member = A.Fake<IGuildMember>();
        }

        public InteractionContextBuilder WithID(Snowflake id)
        {
            this.id = id;
            return this;
        }

        public InteractionContextBuilder WithGuildID(Snowflake guildID)
        {
            this.guildID = guildID;
            return this;
        }

        public InteractionContextBuilder WithGuildUser(IUser user)
        {
            this.user = user;
            return this;
        }

        public InteractionContextBuilder WithToken(string token)
        {
            this.token = token;
            return this;
        }

        public InteractionContext Build()
        {
            A.CallTo(() => member.User).Returns(new Optional<IUser>(user));

            return new(new Interaction(
                               id,
                               ApplicationID: new Snowflake(faker.Random.ULong()),
                               Type: InteractionType.ApplicationCommand,
                               Data: default,
                               GuildID: guildID,
                               Channel: default,
                               ChannelID: default,
                               Member: new Optional<IGuildMember>(member),
                               User: default,
                               Token: token,
                               Version: 1));
        }
    }
}
