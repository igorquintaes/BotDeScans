using BotDeScans.App.Features.Titles.Create;
using BotDeScans.App.Models.Entities;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace BotDeScans.IntegrationTests.Specs.Features.Titles.Create;

[Collection(IntegrationTestCollection.Name)]
public sealed class CommandsTests(IntegrationTestFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task ExecuteAsync_should_create_the_title_modal_successfully()
    {
        Fixture.WireMock
            .Given(Request.Create()
                .WithPath("/api/v10/interactions/*/*/callback")
                .UsingPost())
            .RespondWith(Response.Create().WithStatusCode(204));

        var interactionContext = CreateInteractionContext();
        var interactionApi = Fixture.Host.Services.GetRequiredService<IDiscordRestInteractionAPI>();
        var command = new Commands(interactionContext, interactionApi);

        var result = await command.ExecuteAsync();

        result.IsSuccess.Should().BeTrue();
        Fixture.WireMock.LogEntries.Should().ContainSingle(entry =>
            entry.RequestMessage.Method == "POST" &&
            entry.RequestMessage.Path.EndsWith("/callback", StringComparison.Ordinal));
    }

    private static InteractionContext CreateInteractionContext()
    {
        var user = A.Fake<IUser>();
        var member = A.Fake<IGuildMember>();
        A.CallTo(() => member.User).Returns(new Optional<IUser>(user));
        A.CallTo(() => member.Roles).Returns([]);

        return new InteractionContext(new Interaction(
            ID: new Snowflake(1),
            ApplicationID: new Snowflake(1),
            Type: InteractionType.ApplicationCommand,
            Data: default,
            GuildID: new Snowflake(1),
            Channel: default,
            ChannelID: default,
            Member: new Optional<IGuildMember>(member),
            User: default,
            Token: "integration-test-token",
            Version: 1,
            Message: default,
            AppPermissions: default!,
            Locale: default,
            GuildLocale: default,
            Entitlements: default!,
            Context: default,
            AuthorizingIntegrationOwners: default));
    }
}
