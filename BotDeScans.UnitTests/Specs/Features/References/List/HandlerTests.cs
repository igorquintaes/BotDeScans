using BotDeScans.App.Features.References.List;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using FluentAssertions.Execution;

namespace BotDeScans.UnitTests.Specs.Features.References.List;

public abstract class HandlerTests : UnitTest
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
                    .With(x => x.References,
                    [
                        new() { Key = ExternalReference.MangaDex, Value = "manga-dex-value" },
                        new() { Key = (ExternalReference)999, Value = "random-value" }
                    ])
                    .Create());
}

        [Fact]
        public async Task GivenReferencesFoundShouldReturnSuccessWithExpectedStringList()
        {
            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            var expectedStrings = new[]
            {
                $"1. MangaDex{Environment.NewLine}manga-dex-value{Environment.NewLine}",
                $"2. 999{Environment.NewLine}random-value{Environment.NewLine}"
            };

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            result.ValueOrDefault?.Should().BeEquivalentTo(expectedStrings);
        }

        [Fact]
        public async Task GivenNoneReferencesFoundShouldReturnSuccessWithExpectedStringList()
        {
            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .Returns(fixture.Build<Title>()
                                .With(x => x.References, [])
                                .Create());

            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            using var _ = new AssertionScope();
            result.Should().BeSuccess();
            result.ValueOrDefault?.Should().BeEquivalentTo(["A obra não contém referências."]);
        }

        [Fact]
        public async Task GivenNullTitleShouldReturnErrorResult()
        {
            A.CallTo(() => fixture
                .FreezeFake<TitleRepository>()
                .GetTitleAsync(titleId, cancellationToken))
                .Returns(null as Title);

            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            result.Should().BeFailure().And.HaveError("Obra não encontrada.");
        }
    }
}
