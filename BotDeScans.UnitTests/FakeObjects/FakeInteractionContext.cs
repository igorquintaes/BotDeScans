using AutoFixture;
using BotDeScans.UnitTests.Extensions;
using FakeItEasy;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
namespace BotDeScans.UnitTests.Builders;

public record FakeInteractionContext : InteractionContext
{
    public FakeInteractionContext(IFixture fixture)
        : base(new Interaction(
            ID: fixture.Create<Snowflake>(),
            ApplicationID: fixture.Create<Snowflake>(),
            Type: InteractionType.ApplicationCommand,
            Data: default,
            GuildID: fixture.Create<Snowflake>(),
            Channel: default,
            ChannelID: default,
            Member: new Optional<IGuildMember>(fixture.FreezeFake<IGuildMember>()),
            User: default,
            Token: fixture.Create<string>(),
            Version: 1,
            Message: default,
            AppPermissions: default!,
            Locale: default,
            GuildLocale: default,
            Entitlements: default!,
            Context: default,
            AuthorizingIntegrationOwners: default))
    {
        fixture.FreezeFake<IUser>();
        A.CallTo(() => fixture.FreezeFake<IGuildMember>().User)
            .Returns(new Optional<IUser>(fixture.FreezeFake<IUser>()));
    }
}
