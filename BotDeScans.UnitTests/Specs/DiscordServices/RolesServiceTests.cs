using AutoBogus;
using BotDeScans.App.Services.Discord;
using BotDeScans.UnitTests.Extensions;
using FakeItEasy;
using FluentAssertions;
using FluentAssertions.Execution;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Linq;
using Xunit;
namespace BotDeScans.UnitTests.Specs.DiscordServices;

public class RolesServiceTests : UnitTest<RolesService>
{
    public RolesServiceTests() => 
        instance = new();

    public class ContainsAtLeastOneOfExpectedRoles : RolesServiceTests
    {
        private readonly IEnumerable<string> requiredRoles;
        private readonly IEnumerable<IRole> guildRoles;
        private readonly IEnumerable<Snowflake> userRoles;

        public ContainsAtLeastOneOfExpectedRoles()
        {
            guildRoles = AutoFaker.Generate<Role>(2);
            requiredRoles = guildRoles.Select(x => x.Name);
            userRoles = guildRoles.Select(x => x.ID).ToList();
        }

        [Theory]
        [InlineData("guildRole1")]
        [InlineData("guildRole1", "guildRole2")]
        public void ShouldReturnErrorWhenNotFoundGuildRolesWithExpectedNames(params string[] guildRolesNames)
        {
            var roles = new List<IRole>();
            var guildRoles = guildRolesNames
                .Select(roleName => new AutoFaker<Role>()
                .RuleFor(role => role.Name, roleName)
                .Generate());

            roles.AddRange(guildRoles);

            var result = instance.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                userRoles);

            using var _ = new AssertionScope();
            var errorMessage = $"Invalid request. No role(s) found in server; {string.Join(", ", requiredRoles)}";
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<NotFoundError>()
                  .Which.Message.Should().Be(errorMessage);
        }

        [Fact]
        public void ShouldReturnErrorWhenThereIsNoneRoleRegisteredInServerAndCommandNeedsARole()
        {
            var result = instance.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                new List<Role>(),
                userRoles);

            using var _ = new AssertionScope();
            var errorMessage = $"Invalid request. No role(s) found in server; {string.Join(", ", requiredRoles)}";
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<NotFoundError>()
                  .Which.Message.Should().Be(errorMessage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ShouldReturnFalseWhenGuildMemberIsNotRelatedWithAnyExpectedRoles(int guildMemberRolesQuantity)
        {
            var rolesIDs = dataGenerator.Random.Snowflake(guildMemberRolesQuantity);
            var result = instance.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                rolesIDs);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeTrue();
            result.Entity.Should().BeFalse();
        }

        [Fact]
        public void ShouldReturnTrueWhenGuildMemberHasExactlyOneOfExpectedSingleRole()
        {
            var randomRole = dataGenerator.PickRandom(guildRoles);
            var requiredRoles = new[] { randomRole.Name };

            var result = instance.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                new List<Snowflake> { randomRole.ID });

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeTrue();
            result.Entity.Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnTrueWhenGuildMemberHasExactlyOneOfExpectedMultipleRoles()
        {
            var randomRole = dataGenerator.PickRandom(guildRoles);
            var result = instance.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                new List<Snowflake> { randomRole.ID });

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeTrue();
            result.Entity.Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnTrueWhenGuildMemberHasOneOfExpectedRolesAndOtherNonRelatedRole()
        {

            var randomRole = dataGenerator.PickRandom(guildRoles);
            var requiredRoles = new[] { randomRole.Name };
            var result = instance.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                new List<Snowflake> { randomRole.ID, dataGenerator.Random.Snowflake() });

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeTrue();
            result.Entity.Should().BeTrue();
        }

        [Fact]
        public void ShouldReturnTrueWhenGuildMemberHasMoreThanOneOfExpectedRolesAndOtherNonRelatedRole()
        {
            var guildMemberRolesIDs = guildRoles.Select(x => x.ID).ToList();
            guildMemberRolesIDs.Add(dataGenerator.Random.Snowflake());

            var result = instance.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                guildMemberRolesIDs);

            using var _ = new AssertionScope();
            result.IsSuccess.Should().BeTrue();
            result.Entity.Should().BeTrue();
        }
    }
}
