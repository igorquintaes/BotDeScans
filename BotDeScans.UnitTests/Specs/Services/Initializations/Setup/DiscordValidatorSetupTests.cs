using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Initializations.Setups;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Services.Initializations.Setup;

public class DiscordValidatorSetupTests : UnitTest
{
    public class ExecuteAsync : DiscordValidatorSetupTests
    {
        private readonly DiscordValidatorSetup setup;

        public ExecuteAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<IValidator<DiscordValidator>>()
                .ValidateAsync(A<DiscordValidator>.Ignored, cancellationToken))
                .Returns(new ValidationResult());

            setup = fixture.Create<DiscordValidatorSetup>();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await setup.ExecuteAsync(cancellationToken);
            result.Should().BeSuccess().And.HaveReason("Discord validation data completed");
        }

        [Fact]
        public async Task GivenValidationErrorShoultReturnErrorResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IValidator<DiscordValidator>>()
                .ValidateAsync(A<DiscordValidator>.Ignored, cancellationToken))
                .Returns(new ValidationResult([
                    new ValidationFailure("prop", "error")]));

            var result = await setup.ExecuteAsync(cancellationToken);
            result.Should().BeFailure().And.HaveError("error");
            result.Errors.Should().ContainSingle();
            result.Errors[0].Metadata.Should().ContainSingle();
            result.Errors[0].Metadata.Should().BeEquivalentTo(new Dictionary<string, object>()
            {
                { "PropertyName", "prop" }
            });
        }
    }

    public class Validator : DiscordValidatorSetupTests
    {
        private readonly string globalPing;

        public Validator()
        {
            globalPing = fixture.Create<string>();

            fixture.FreezeFake<RolesService>();
            fixture.FreezeFakeConfiguration("Discord:Token", fixture.Create<string>());
            fixture.FreezeFakeConfiguration("Discord:ReleaseChannel", fixture.Create<ulong>().ToString());
            fixture.FreezeFakeConfiguration("Discord:ServerId", fixture.Create<ulong>().ToString());
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Global.ToString());
            fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, globalPing);
        }

        [Fact]
        public async Task GivenValidDataShouldReturnSuccess()
        {
            var result = await fixture
                .Create<DiscordValidatorSetupValidator>()
                .TestValidateAsync(fixture.Freeze<DiscordValidator>(), default, cancellationToken);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [MemberData(nameof(NonGlobalPingType))]
        public async Task GivenNotGlobalPingTypeShouldNotCheckGlobalRoleExistence(PingType pingType)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());

            var result = await fixture
                .Create<DiscordValidatorSetupValidator>()
                .TestValidateAsync(fixture.Freeze<DiscordValidator>(), default, cancellationToken);

            result.ShouldNotHaveAnyValidationErrors();

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(A<string>.Ignored, cancellationToken))
                .MustNotHaveHappened();
        }

        public static TheoryData<PingType> NonGlobalPingType() =>
            [.. Enum.GetValues<PingType>().Where(p => p != PingType.Global)];

        [Theory]
        [InlineData("Discord:Token")]
        [InlineData("Discord:ReleaseChannel")]
        [InlineData("Discord:ServerId")]
        [InlineData(Ping.PING_TYPE_KEY)]
        [InlineData(GlobalPing.GLOBAL_ROLE_KEY)]
        public async Task GivenMissingKeyShouldReturnError(string key)
        {
            fixture.FreezeFakeConfiguration(key, default(string));

            var result = await fixture
                .Create<DiscordValidatorSetupValidator>()
                .TestValidateAsync(fixture.Freeze<DiscordValidator>(), default, cancellationToken);

            result.ShouldHaveValidationErrorFor(service => service)
                .WithErrorMessage($"'{key}' config value not found.")
                .Only();
        }

        [Fact]
        public async Task GivenErrorToGetRoleShouldReturnValidarionError()
        {
            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetRoleAsync(globalPing, cancellationToken))
                .Returns(Result.Fail(["err-1", "err-2"]));

            var result = await fixture
                .Create<DiscordValidatorSetupValidator>()
                .TestValidateAsync(fixture.Freeze<DiscordValidator>(), default, cancellationToken);

            result.ShouldHaveValidationErrorFor(service => service)
                .WithErrorMessage("err-1; err-2")
                .Only();
        }
    }
}
