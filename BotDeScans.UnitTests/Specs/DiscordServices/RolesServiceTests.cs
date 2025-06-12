using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Discord.Cache;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
namespace BotDeScans.UnitTests.Specs.DiscordServices;

public class RolesServiceTests : UnitTest
{
    public RolesServiceTests()
    {
        fixture.FreezeFake<IConfiguration>();
        fixture.FreezeFake<IDiscordRestGuildAPI>();
        fixture.Inject<ScopedRoleCache>(new());
        fixture.Inject<IRole>(fixture.Create<Role>());
    }

    public class GetRoleAsync : RolesServiceTests
    {
        private readonly RolesService service;

        public GetRoleAsync()
        {
            fixture.FreezeFakeConfiguration(
                RolesService.DISCORD_SERVERID_KEY,
                fixture.Create<ulong>().ToString());

            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestGuildAPI>()
                .GetGuildRolesAsync(A<Snowflake>.Ignored, cancellationToken))
                .Returns(Result<IReadOnlyList<IRole>>.FromSuccess([fixture.Freeze<IRole>()]));

            service = fixture.Create<RolesService>();
        }

        [Fact]
        public async Task GivenNeedsCacheTrueShouldCallApiAndUpdateCache()
        {
            var result = await service.GetRoleAsync(
                fixture.Freeze<IRole>().Name,
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<IRole>());
            fixture.Freeze<ScopedRoleCache>().Roles.Should().Contain(fixture.Freeze<IRole>());
        }

        [Fact]
        public async Task GivenNeedsCacheFalseShouldNotCallApiAndUseCache()
        {
            fixture.Freeze<ScopedRoleCache>().Roles = [fixture.Freeze<IRole>()];

            var result = await service.GetRoleAsync(
                fixture.Freeze<IRole>().Name,
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<IRole>());

            A.CallTo(() => fixture
                .FreezeFake<IDiscordRestGuildAPI>()
                .GetGuildRolesAsync(A<Snowflake>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenSuccessSearchByNameShouldReturnSuccessResultWithRoles()
        {
            var result = await service.GetRoleAsync(
                fixture.Freeze<IRole>().Name,
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<IRole>());
        }

        [Fact]
        public async Task GivenSuccessSearchByIdShouldReturnSuccessResultWithRoles()
        {
            var result = await service.GetRoleAsync(
                fixture.Freeze<IRole>().ID.ToString(),
                cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<IRole>());
        }

        [Fact]
        public async Task GivenErrorToGetGuildRolesFromApiShouldReturnFailResult()
        {
            const string ERROR_MESSAGE = "error message";

            A.CallTo(() => fixture
               .FreezeFake<IDiscordRestGuildAPI>()
                .GetGuildRolesAsync(A<Snowflake>.Ignored, cancellationToken))
                .Returns(Result<IReadOnlyList<IRole>>.FromError(new InvalidOperationError(ERROR_MESSAGE)));

            var result = await service.GetRoleAsync(
                fixture.Freeze<IRole>().Name,
                cancellationToken);

            result.Should().BeFailure().And.HaveError(ERROR_MESSAGE);
        }

        [Fact]
        public async Task GivenNotFoundRoleShouldReturnFailResult()
        {
            var roleToGet = fixture.Create<string>();
            var result = await service.GetRoleAsync(
                roleToGet,
                cancellationToken);

            result.Should().BeFailure().And.HaveError($"Cargo não encontrado no servidor: {roleToGet}");
        }
    }

    public class GetOptionalRoleAsync : RolesServiceTests
    {
        private readonly RolesService service;

        public GetOptionalRoleAsync()
        {
            service = fixture.FreezeFake<RolesService>();

            A.CallTo(() => service
                .GetOptionalRoleAsync(A<string?>.Ignored, cancellationToken))
                .CallsBaseMethod();

            A.CallTo(() => service
                .GetRoleAsync(A<string>.Ignored!, cancellationToken))
                .Returns(FluentResults.Result.Ok(fixture.Freeze<IRole>()));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GivenEmptyRoleShouldReturnSuccessResultWithNull(string? roleToGet)
        {
            var result = await service.GetOptionalRoleAsync(roleToGet, cancellationToken);
            result.Should().BeSuccess().And.HaveValue(null);
        }
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GivenEmptyRoleShouldNotEvenCallDiscordApi(string? roleToGet)
        {
            await service.GetOptionalRoleAsync(roleToGet, cancellationToken);

            A.CallTo(() => service
                .GetRoleAsync(A<string>.Ignored!, cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenValidRoleShouldReturnSuccessResultWithRole()
        {
            var roleToGet = fixture.Freeze<IRole>().Name;
            var result = await service.GetOptionalRoleAsync(roleToGet, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<IRole>());
        }

        [Fact]
        public async Task GivenValidRoleShouldCallDiscordApi()
        {
            var roleToGet = fixture.Freeze<IRole>().Name;
            var result = await service.GetOptionalRoleAsync(roleToGet, cancellationToken);

            result.Should().BeSuccess().And.HaveValue(fixture.Freeze<IRole>());

            A.CallTo(() => service
                .GetRoleAsync(roleToGet, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }
    }
}
