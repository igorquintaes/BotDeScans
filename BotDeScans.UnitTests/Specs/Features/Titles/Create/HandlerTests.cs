using BotDeScans.App.Features.Titles.Create;
using BotDeScans.App.Infra;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Features.Titles.Create;

public class HandlerTests : UnitTest
{
    public readonly Handler handler;

    public HandlerTests()
    {
        fixture.FreezeFake<DatabaseContext>();
        fixture.FreezeFake<RolesService>();
        fixture.FreezeFake<IValidator<Title>>();

        handler = fixture.Create<Handler>();
    }

    public class ExecuteAsync : HandlerTests
    {
        public ExecuteAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetOptionalRoleAsync(A<string>.Ignored, A<CancellationToken>._))
                .Returns(Result.Ok<IRole?>(fixture.FreezeFake<IRole>()));

            A.CallTo(() => fixture
                .FreezeFake<IRole>().ID)
                .Returns(fixture.Freeze<Snowflake>());

            A.CallTo(() => fixture
                .FreezeFake<IValidator<Title>>()
                .ValidateAsync(A<Title>.Ignored, cancellationToken))
                .Returns(new FluentValidation.Results.ValidationResult());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await handler.ExecuteAsync(
                fixture.Create<string>(), 
                fixture.Create<string>(), 
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldPersistExpectedTitle()
        {
            var name = fixture.Create<string>();
            var role = fixture.Create<string>();

            await handler.ExecuteAsync(name, role, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .AddAsync(
                    A<Title>.That.Matches(x => x.Name == name && x.DiscordRoleId == fixture.Freeze<Snowflake>().Value),
                    cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .SaveChangesAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenNullRoleShouldPersistNullTitleDiscordRoleId()
        {
            var name = fixture.Create<string>();
            var role = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetOptionalRoleAsync(A<string>.Ignored, A<CancellationToken>._))
                .Returns(Result.Ok<IRole?>(null));

            await handler.ExecuteAsync(name, role, cancellationToken);

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .AddAsync(
                    A<Title>.That.Matches(x => x.Name == name && x.DiscordRoleId == null),
                    cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<DatabaseContext>()
                .SaveChangesAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenValidationErrorShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IValidator<Title>>()
                .ValidateAsync(A<Title>.Ignored, cancellationToken))
                .Returns(new FluentValidation.Results.ValidationResult(
                    [new FluentValidation.Results.ValidationFailure("Name", "some error.")]));

            var result = await handler.ExecuteAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("some error.");
        }

        [Fact]
        public async Task GivenErrorToObtainDiscordRoleShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetOptionalRoleAsync(A<string>.Ignored, A<CancellationToken>._))
                .Returns(Result.Fail("some error."));

            var result = await handler.ExecuteAsync(
                fixture.Create<string>(),
                fixture.Create<string>(),
                cancellationToken);

            result.Should().BeFailure().And.HaveError("some error.");
        }
    }
}