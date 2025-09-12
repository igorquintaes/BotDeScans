using BotDeScans.App.Features.Titles.Update;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Features.Titles.Update;

public class HandlerTests : UnitTest
{
    public readonly Handler handler;

    public HandlerTests()
    {
        fixture.FreezeFake<TitleRepository>();
        fixture.FreezeFake<RolesService>();
        fixture.FreezeFake<IValidator<Title>>();

        handler = fixture.Create<Handler>();
    }

    public class ExecuteAsync : HandlerTests
    {
        private readonly int titleId;

        public ExecuteAsync()
        {
            titleId = fixture.Create<int>();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .Returns(fixture.Freeze<Title>());

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetOptionalRoleAsync(A<string?>.Ignored, A<CancellationToken>._))
                .Returns(Result.Ok<IRole?>(fixture.FreezeFake<IRole>()));

            A.CallTo(() => fixture
                .FreezeFake<IRole>().ID)
                .Returns(fixture.Freeze<Snowflake>());

            A.CallTo(() => fixture
                .FreezeFake<IValidator<Title>>()
                .ValidateAsync(A<Title>.Ignored, cancellationToken))
                .Returns(new ValidationResult());
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldReturnSuccessResult()
        {
            var result = await handler.ExecuteAsync(
                fixture.Create<string>(),
                fixture.Create<string?>(),
                titleId,
                cancellationToken);

            result.Should().BeSuccess();
        }

        [Fact]
        public async Task GivenSuccessfulExecutionShouldUpdateTitleAndSave()
        {
            var name = fixture.Create<string>();
            var role = fixture.Create<string>();

            await handler.ExecuteAsync(name, role, titleId, cancellationToken);

            fixture.Freeze<Title>().Name.Should().Be(name);
            fixture.Freeze<Title>().DiscordRoleId.Should().Be(fixture.Freeze<Snowflake>().Value);

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .SaveAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenNullDiscordRoleShouldUpdateTitleAndSave()
        {
            var name = fixture.Create<string>();
            var role = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetOptionalRoleAsync(A<string?>.Ignored, A<CancellationToken>._))
                .Returns(Result.Ok<IRole?>(null));

            await handler.ExecuteAsync(name, role, titleId, cancellationToken);

            fixture.Freeze<Title>().Name.Should().Be(name);
            fixture.Freeze<Title>().DiscordRoleId.Should().BeNull();

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .SaveAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenValidationErrorShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<IValidator<Title>>()
                .ValidateAsync(A<Title>.Ignored, cancellationToken))
                .Returns(new ValidationResult(
                    [new ValidationFailure("Name", "some error.")]));

            var result = await handler.ExecuteAsync(
                fixture.Create<string>(),
                fixture.Create<string?>(),
                titleId,
                cancellationToken);

            result.Should().BeFailure().And.HaveError("some error.");

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .SaveAsync(cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenErrorToObtainDiscordRoleShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<RolesService>()
                .GetOptionalRoleAsync(A<string?>.Ignored, A<CancellationToken>._))
                .Returns(Result.Fail("some error."));

            var result = await handler.ExecuteAsync(
                fixture.Create<string>(),
                fixture.Create<string?>(),
                titleId,
                cancellationToken);

            result.Should().BeFailure().And.HaveError("some error.");

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .SaveAsync(cancellationToken))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task GivenTitleNotFoundShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .Returns((Title?)null);

            var result = await handler.ExecuteAsync(
                fixture.Create<string>(),
                fixture.Create<string?>(),
                titleId,
                cancellationToken);

            result.Should().BeFailure().And.HaveError("Obra não encontrada."); 

            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .SaveAsync(cancellationToken))
                .MustNotHaveHappened();
        }
    }
}
