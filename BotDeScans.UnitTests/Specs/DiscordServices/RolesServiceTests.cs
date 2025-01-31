using AutoBogus;
using BotDeScans.App.Services.Discord;
using BotDeScans.UnitTests.Extensions;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using System.Collections.Generic;
using System.Linq;
using Xunit;
namespace BotDeScans.UnitTests.Specs.DiscordServices;

public class RolesServiceTests : UnitTest
{
    private readonly RolesService service = new(null!, null!);

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

            var expectedErrorMessage = $"Invalid request. No role(s) found in server; {string.Join(", ", requiredRoles)}";

            var result = service.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                userRoles);

            result.Should().BeFailure().And.HaveError(expectedErrorMessage);
        }

        [Fact]
        public void ShouldReturnErrorWhenThereIsNoneRoleRegisteredInServerAndCommandNeedsARole()
        {
            var result = service.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                new List<Role>(),
                userRoles);

            var errorMessage = $"Invalid request. No role(s) found in server; {string.Join(", ", requiredRoles)}";
            result.Should().BeFailure().And.HaveError(errorMessage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ShouldReturnFalseWhenGuildMemberIsNotRelatedWithAnyExpectedRoles(int guildMemberRolesQuantity)
        {
            var rolesIDs = dataGenerator.Random.Snowflake(guildMemberRolesQuantity);
            var result = service.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                rolesIDs);

            result.Should().BeSuccess().And.HaveValue(false);
        }

        [Fact]
        public void ShouldReturnTrueWhenGuildMemberHasExactlyOneOfExpectedSingleRole()
        {
            var randomRole = dataGenerator.PickRandom(guildRoles);
            var requiredRoles = new[] { randomRole.Name };

            var result = service.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                new List<Snowflake> { randomRole.ID });

            result.Should().BeSuccess().And.HaveValue(true);
        }

        [Fact]
        public void ShouldReturnTrueWhenGuildMemberHasExactlyOneOfExpectedMultipleRoles()
        {
            var randomRole = dataGenerator.PickRandom(guildRoles);
            var result = service.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                new List<Snowflake> { randomRole.ID });

            result.Should().BeSuccess().And.HaveValue(true);
        }

        [Fact]
        public void ShouldReturnTrueWhenGuildMemberHasOneOfExpectedRolesAndOtherNonRelatedRole()
        {

            var randomRole = dataGenerator.PickRandom(guildRoles);
            var requiredRoles = new[] { randomRole.Name };
            var result = service.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                new List<Snowflake> { randomRole.ID, dataGenerator.Random.Snowflake() });

            result.Should().BeSuccess().And.HaveValue(true);
        }

        [Fact]
        public void ShouldReturnTrueWhenGuildMemberHasMoreThanOneOfExpectedRolesAndOtherNonRelatedRole()
        {
            var guildMemberRolesIDs = guildRoles.Select(x => x.ID).ToList();
            guildMemberRolesIDs.Add(dataGenerator.Random.Snowflake());

            var result = service.ContainsAtLeastOneOfExpectedRoles(
                requiredRoles,
                guildRoles,
                guildMemberRolesIDs);

            result.Should().BeSuccess().And.HaveValue(true);
        }
    }
}
