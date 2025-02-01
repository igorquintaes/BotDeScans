using AutoFixture;
using BotDeScans.App.Attributes;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Discord.Conditions;
using BotDeScans.UnitTests.Builders;
using BotDeScans.UnitTests.Extensions;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Conditions;

public class RoleAuthorizeConditionTests : UnitTest
{
    private readonly RoleAuthorizeCondition condition;

    public RoleAuthorizeConditionTests()
    {
        fixture.FreezeFake<IDiscordRestGuildAPI>();
        fixture.FreezeFake<IDiscordRestInteractionAPI>();
        fixture.FreezeFake<RolesService>();
        fixture.Inject<IOperationContext>(new InteractionContextFake(fixture));

        condition = fixture.Create<RoleAuthorizeCondition>();
    }

    public class CheckAsync : RoleAuthorizeConditionTests
    {
        public CheckAsync()
        {
            A.CallTo(() => fixture.FreezeFake<IGuildMember>().Roles)
                .Returns([fixture.FreezeFake<IRole>().ID]);

            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestGuildAPI>()
                .GetGuildMemberAsync(A<Snowflake>.Ignored, A<Snowflake>.Ignored, cancellationToken))
                .Returns(Result<IGuildMember>.FromSuccess(fixture.FreezeFake<IGuildMember>()));

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleFromGuildAsync(A<string>.Ignored, cancellationToken))
                .Returns(FluentResults.Result.Ok(fixture.FreezeFake<IRole>()));

            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestInteractionAPI>()
                .CreateInteractionResponseAsync(A<Snowflake>.Ignored, A<string>.Ignored, A<InteractionResponse>.Ignored, default, cancellationToken))
                .Returns(Result.FromSuccess());
        }

        [Fact]
        public async Task GivenSuccessfulCallShouldReturnSuccess()
        {
            var result = await condition.CheckAsync(
                new RoleAuthorizeAttribute(fixture.FreezeFake<IRole>().Name),
                cancellationToken);

            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestInteractionAPI>()
                .CreateInteractionResponseAsync(A<Snowflake>.Ignored, A<string>.Ignored, A<InteractionResponse>.Ignored, default, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenInvalidContextShouldReturnError()
        {
            var newCondition = new RoleAuthorizeCondition(
                fixture.FreezeFake<IMessageContext>(),
                fixture.FreezeFake<IDiscordRestGuildAPI>(),
                fixture.FreezeFake<IDiscordRestInteractionAPI>(),
                fixture.FreezeFake<RolesService>());

            var result = await newCondition.CheckAsync(default!, cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error?.Message.Should().Be("slash-command is mandatory!");
        }

        [Fact]
        public async Task GivenErrorToObtainGuildMemberShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestGuildAPI>()
                .GetGuildMemberAsync(
                    fixture.Freeze<IOperationContext>().As<InteractionContext>().Interaction.GuildID.Value,
                    fixture.Freeze<IOperationContext>().As<InteractionContext>().Interaction.Member.Value.User.Value.ID,
                    cancellationToken))
                .Returns(Result<IGuildMember>.FromError(new InvalidOperationError("error message")));

            var result = await condition.CheckAsync(default!, cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error?.Message.Should().Be("error message");
        }

        [Fact]
        public async Task GivenErrorToObtainRolesFromDiscordShouldReturnError()
        {
            var attribute = new RoleAuthorizeAttribute(fixture.FreezeFake<IRole>().Name);

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleFromGuildAsync(
                    A<string>.That.IsEqualTo(fixture.FreezeFake<IRole>().Name),
                    cancellationToken))
                .Returns(FluentResults.Result.Fail<IRole>("error message"));

            var result = await condition.CheckAsync(attribute, cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error?.Message.Should().Be("[{\"Message\":\"error message\",\"Number\":1,\"Depth\":0,\"Type\":0}]");
        }

        [Fact]
        public async Task GivenNoneRoleFoundShouldReturnErrorAndSendAnInteractionResponse()
        {
            var attribute = new RoleAuthorizeAttribute(fixture.FreezeFake<IRole>().Name);
            var guildRole = fixture
                .Build<Role>()
                .With(x => x.ID, fixture.Create<Snowflake>())
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleFromGuildAsync(
                    A<string>.That.IsEqualTo(fixture.FreezeFake<IRole>().Name),
                    cancellationToken))
                .Returns(FluentResults.Result.Ok<IRole>(guildRole));

            var result = await condition.CheckAsync(attribute, cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error?.Message.Should().Be(
                $"Invalid request for user: {fixture.FreezeFake<IUser>().Username}, Id: {fixture.FreezeFake<IUser>().ID}. " +
                $"No role authorization for the user.");

            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestInteractionAPI>()
                .CreateInteractionResponseAsync(
                    fixture.Freeze<IOperationContext>().As<InteractionContext>().Interaction.ID,
                    fixture.Freeze<IOperationContext>().As<InteractionContext>().Interaction.Token,
                    A<InteractionResponse>.That.IsNotNull(),
                    default,
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToSendInteractionResponseShouldReturnItsError()
        {
            var attribute = new RoleAuthorizeAttribute(fixture.FreezeFake<IRole>().Name);

            var guildRole = fixture
                .Build<Role>()
                .With(x => x.ID, fixture.Create<Snowflake>())
                .Create();

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleFromGuildAsync(
                    A<string>.That.IsEqualTo(fixture.FreezeFake<IRole>().Name),
                    cancellationToken))
                .Returns(FluentResults.Result.Ok<IRole>(guildRole));

            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestInteractionAPI>()
                .CreateInteractionResponseAsync(
                    fixture.Freeze<IOperationContext>().As<InteractionContext>().Interaction.ID,
                    fixture.Freeze<IOperationContext>().As<InteractionContext>().Interaction.Token,
                    A<InteractionResponse>.That.IsNotNull(),
                    default,
                    cancellationToken))
                .Returns(Result.FromError(new InvalidOperationError("error message")));

            var result = await condition.CheckAsync(attribute, cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error?.Message.Should().Be("error message");
        }
    }
}
