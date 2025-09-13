using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Titles.SkipSteps.List;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;
using FluentAssertions.Execution;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps.List;

public class HandlerTests : UnitTest
{
    private readonly Handler handler;

    public HandlerTests()
    {
        fixture.FreezeFake<TitleRepository>();

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
                .Returns(fixture
                    .Build<Title>()
                    .With(x => x.SkipSteps,
                    [
                        new () { Step = StepName.UploadMangadex },
                        new () { Step = StepName.UploadSakuraMangas }
                    ])
                    .Create());
        }

        [Fact]
        public async Task GivenStepNamesFoundShouldReturnSuccessWithExpectedStringList()
        {
            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            var expectedStrings = new[]
            {
                $"1. {StepName.UploadMangadex.GetDescription()}",
                $"2. {StepName.UploadSakuraMangas.GetDescription()}"
            };

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            result.ValueOrDefault?.Should().BeEquivalentTo(expectedStrings);
        }

        [Fact]
        public async Task GivenNoReferencesFoundShouldReturnSuccessWithExpectedStringList()
        {
            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .Returns(fixture
                    .Build<Title>()
                    .With(x => x.SkipSteps, [])
                    .Create());

            var expectedStrings = new[] { "A obra não contém procedimentos de publicação a serem ignorados." };

            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            result.ValueOrDefault?.Should().BeEquivalentTo(expectedStrings);
        }

        [Fact]
        public async Task GivenNullTitleShouldReturnFailResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .Returns(null as Title);

            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeFailure().And.HaveError("Obra não encontrada.");
        }
    }
}
