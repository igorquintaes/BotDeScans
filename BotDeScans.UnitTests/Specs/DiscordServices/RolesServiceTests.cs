﻿using BotDeScans.App.Services.Discord;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
namespace BotDeScans.UnitTests.Specs.DiscordServices;

public class RolesServiceTests : UnitTest
{
    private readonly RolesService service;

    public RolesServiceTests()
    {
        fixture.FreezeFake<IDiscordRestGuildAPI>();
        fixture.Inject<IRole>(fixture.Create<Role>());

        fixture.FreezeFakeConfiguration(
            RolesService.DISCORD_SERVERID_KEY,
            fixture.Create<ulong>().ToString());

        A.CallTo(() => fixture
            .FreezeFake<IDiscordRestGuildAPI>()
            .GetGuildRolesAsync(A<Snowflake>.Ignored, cancellationToken))
            .Returns(Result<IReadOnlyList<IRole>>.FromSuccess([fixture.Freeze<IRole>()]));

        service = fixture.Create<RolesService>();
    }

    public class GetRoleFromGuildAsync : RolesServiceTests
    {
        [Fact]
        public async Task GivenSuccessSearchByNameShouldReturnSuccessResultWithRoles()
        {
            var result = await service.GetRoleFromGuildAsync(
                fixture.Freeze<IRole>().Name,
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<IRole>());
        }

        [Fact]
        public async Task GivenSuccessSearchByIdShouldReturnSuccessResultWithRoles()
        {
            var result = await service.GetRoleFromGuildAsync(
                fixture.Freeze<IRole>().ID.ToString(),
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<IRole>());
        }

        [Fact]
        public async Task GivenErroroGetGuildRolesFromApiShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "error message";

            A.CallTo(() => fixture
               .FreezeFake<IDiscordRestGuildAPI>()
                .GetGuildRolesAsync(A<Snowflake>.Ignored, cancellationToken))
                .Returns(Result<IReadOnlyList<IRole>>.FromError(new InvalidOperationError(ERROR_MESSAGE)));

            var result = await service.GetRoleFromGuildAsync(
                fixture.Freeze<IRole>().Name,
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenNotFoundRoleShouldReturnFailResult()
        {
            var roleToGet = fixture.Create<string>();
            var result = await service.GetRoleFromGuildAsync(
                roleToGet,
                cancellationToken);

            result.Should().BeFailure().And.HaveError($"Cargo não encontrado no servidor: {roleToGet}");
        }
    }
}
