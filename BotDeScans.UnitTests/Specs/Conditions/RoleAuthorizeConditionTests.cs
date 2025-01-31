using AutoBogus;
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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Conditions;

public class RoleAuthorizeConditionTests : UnitTest
{
    private readonly RoleAuthorizeCondition condition;

    private readonly Snowflake userID;
    private readonly Snowflake guildID;
    private readonly IReadOnlyList<IRole> guildRoles;
    private readonly IGuildMember guildMember;

    private readonly RoleAuthorizeAttribute roleAuthorizeAttribute;
    private readonly InteractionContext interactionContext;
    private readonly RolesService rolesService;
    private readonly IDiscordRestGuildAPI discordRestGuildAPI;
    private readonly IDiscordRestInteractionAPI restInteractionAPI;

    public RoleAuthorizeConditionTests()
    {
        cancellationToken = new();
        userID = dataGenerator.Random.Snowflake();
        guildRoles = AutoFaker.Generate<Role>(2);
        roleAuthorizeAttribute = new(AutoFaker.Generate<string>(3).ToArray());
        guildID = dataGenerator.Random.Snowflake();
        var commandID = dataGenerator.Random.Snowflake();
        var token = dataGenerator.Random.String();

        restInteractionAPI = A.Fake<IDiscordRestInteractionAPI>();
        discordRestGuildAPI = A.Fake<IDiscordRestGuildAPI>();
        guildMember = A.Fake<IGuildMember>();
        rolesService = A.Fake<RolesService>();
        var user = A.Fake<IUser>();

        A.CallTo(() => user.ID)
            .Returns(userID);

        A.CallTo(() => discordRestGuildAPI
            .GetGuildMemberAsync(guildID, userID, cancellationToken))
            .Returns(Task.FromResult(Result<IGuildMember>.FromSuccess(guildMember)));

        A.CallTo(() => discordRestGuildAPI
            .GetGuildRolesAsync(guildID, cancellationToken))
            .Returns(Task.FromResult(Result<IReadOnlyList<IRole>>
                .FromSuccess(guildRoles)));

        A.CallTo(() => guildMember
            .Roles)
            .Returns([dataGenerator.Random.Snowflake()]);

        A.CallTo(() => rolesService
            .ContainsAtLeastOneOfExpectedRoles(roleAuthorizeAttribute.RoleNames, guildRoles, guildMember.Roles))
            .Returns(FluentResults.Result.Ok(true));

        A.CallTo(() => restInteractionAPI
            .CreateInteractionResponseAsync(commandID, token, A<InteractionResponse>.Ignored, default, cancellationToken))
            .Returns(Task.FromResult(Result.FromSuccess()));

        interactionContext = new InteractionContextBuilder()
            .WithID(commandID)
            .WithToken(token)
            .WithGuildID(guildID)
            .WithGuildUser(user)
            .Build();

        condition = new(
            interactionContext,
            discordRestGuildAPI,
            restInteractionAPI,
            rolesService);
    }

    public class CheckAsync : RoleAuthorizeConditionTests
    {
        [Fact]
        public async Task ShouldReturnErrorWhenContextIsNotSlashCommand()
        {
            var condition = new RoleAuthorizeCondition(
                A.Fake<ICommandContext>(),
                A.Fake<IDiscordRestGuildAPI>(),
                A.Fake<IDiscordRestInteractionAPI>(),
                A.Fake<RolesService>());

            var result = await condition.CheckAsync(
                roleAuthorizeAttribute, 
                cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<InvalidOperationError>()
                  .Which.Message.Should().Be("slash-command is mandatory!");
        }

        [Fact]
        public async Task ShouldReturnErrorIfGetGuildMemberAsyncReturnsAnInvalidResponse()
        {
            var errorResult = Result<IGuildMember>.FromError(new Exception());
            A.CallTo(() => discordRestGuildAPI
                .GetGuildMemberAsync(guildID, userID, cancellationToken))
                .Returns(Task.FromResult(errorResult));

            var result = await condition.CheckAsync(
                roleAuthorizeAttribute,
                cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error.As<ExceptionError>().Should().Be(errorResult.Error);
        }

        [Fact]
        public async Task ShouldReturnErrorIfGetGuildRolesAsyncReturnsAnInvalidResponse()
        {
            var errorResult = Result<IReadOnlyList<IRole>>.FromError(new Exception());
            A.CallTo(() => discordRestGuildAPI
                .GetGuildRolesAsync(guildID, cancellationToken))
                .Returns(Task.FromResult(errorResult));

            var result = await condition.CheckAsync(
                roleAuthorizeAttribute,
                cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error.As<ExceptionError>().Should().Be(errorResult.Error);
        }

        [Fact]
        public async Task ShouldReturnErrorIfContainsAtLeastOneOfExpectedRolesReturnsAnErrorResponse()
        {
            const string ERROR_MESSAGE = "some error";
            A.CallTo(() => rolesService
                .ContainsAtLeastOneOfExpectedRoles(roleAuthorizeAttribute.RoleNames, guildRoles, guildMember.Roles))
                .Returns(FluentResults.Result.Fail(ERROR_MESSAGE));

            var result = await condition.CheckAsync(
                roleAuthorizeAttribute,
                cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error?.Message.Should().Be(ERROR_MESSAGE);
        }

        [Fact]
        public async Task ShouldReturnErrorWhenDiscordApiFailsToCreateUnauthorizedResponse()
        {
            A.CallTo(() => rolesService
                .ContainsAtLeastOneOfExpectedRoles(roleAuthorizeAttribute.RoleNames, guildRoles, guildMember.Roles))
                .Returns(FluentResults.Result.Ok(false));

            var errorResult = Result<bool>.FromError(new Exception());
            A.CallTo(() => restInteractionAPI
                .CreateInteractionResponseAsync(
                    A<Snowflake>.Ignored,
                    A<string>.Ignored,
                    A<InteractionResponse>.Ignored,
                    default,
                    cancellationToken))
                .Returns(Task.FromResult(Result.FromError(errorResult)));

            var result = await condition.CheckAsync(
                roleAuthorizeAttribute,
                cancellationToken);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeFalse();
            result.Error.As<ExceptionError>().Should().Be(errorResult.Error);
        }

        [Fact]
        public async Task ShouldCreateResponseWhenUserHasNoneOfExpectedRoles()
        {
            A.CallTo(() => rolesService
                .ContainsAtLeastOneOfExpectedRoles(roleAuthorizeAttribute.RoleNames, guildRoles, guildMember.Roles))
                .Returns(FluentResults.Result.Ok(false));

            var result = await condition.CheckAsync(
                roleAuthorizeAttribute,
                cancellationToken);

            A.CallTo(() => rolesService
                .ContainsAtLeastOneOfExpectedRoles(roleAuthorizeAttribute.RoleNames, guildRoles, guildMember.Roles))
                .MustHaveHappenedOnceExactly()
                .Then(
            A.CallTo(() => restInteractionAPI
                .CreateInteractionResponseAsync(
                    interactionContext.Interaction.ID,
                    interactionContext.Interaction.Token,
                    A<InteractionResponse>.That.Matches(response =>
                        response.Type == InteractionCallbackType.ChannelMessageWithSource &&
                        response.Data.Value.AsT0.Embeds.Value.Single().Title == "Unauthorized!" &&
                        response.Data.Value.AsT0.Embeds.Value.Single().Description == $"You aren't in any of {string.Join(", ", roleAuthorizeAttribute.RoleNames)} role(s)!" &&
                        response.Data.Value.AsT0.Embeds.Value.Single().Colour == Color.Red),
                    default,
                    cancellationToken))
                .MustHaveHappenedOnceExactly());
        }

        [Fact]
        public async Task ShouldReturnErrorWhenUserHasNotInExpectedRole()
        {
            A.CallTo(() => rolesService
                .ContainsAtLeastOneOfExpectedRoles(roleAuthorizeAttribute.RoleNames, guildRoles, guildMember.Roles))
                .Returns(FluentResults.Result.Ok(false));

            A.CallTo(() => guildMember.Roles)
                .Returns(new List<Snowflake> { guildRoles[0].ID });

            var result = await condition.CheckAsync(
                roleAuthorizeAttribute,
                cancellationToken);

            using var _ = new AssertionScope();
            var errorMessage = $"Invalid request for {userID} user. No role authorization for the user.";
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<InvalidOperationError>()
                  .Which.Message.Should().Be(errorMessage);
        }

        [Fact]
        public async Task ShouldReturnSuccessIfUserHasTheExpectedRole()
        {
            var result = await condition.CheckAsync(
                roleAuthorizeAttribute,
                cancellationToken);

            result.IsSuccess.Should().BeTrue();
        }
    }
}
