﻿using BotDeScans.App.Features.References.List;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.References.List;

public class HandlerTests : UnitTest
{
    public readonly Handler handler;

    public HandlerTests()
    {
        fixture.FreezeFake<Persistence>();

        handler = fixture.Create<Handler>();
    }

    public class ExecuteAsync : HandlerTests
    {
        private readonly int titleId;

        public ExecuteAsync()
        {
            titleId = fixture.Create<int>();

            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetReferencesAsync(titleId, cancellationToken))
                .Returns(
                [
                    new() { Key = ExternalReference.MangaDex, Value = "manga-dex-value" },
                    new() { Key = (ExternalReference)999, Value = "random-value" },
                ]);
        }

        [Fact]
        public async Task GivenReferencesFoundShouldReturnExpectedStringList()
        {
            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            var expectedStrings = new[]
            {
                $"1. MangaDex{Environment.NewLine}manga-dex-value{Environment.NewLine}",
                $"2. 999{Environment.NewLine}random-value{Environment.NewLine}"
            };

            result.Should().BeEquivalentTo(expectedStrings);
        }

        [Fact]
        public async Task GivenNoReferencesFoundShouldReturnExpectedStringList()
        {
            A.CallTo(() => fixture
                .FreezeFake<Persistence>()
                .GetReferencesAsync(titleId, cancellationToken))
                .Returns([]);

            var result = await handler.ExecuteAsync(titleId, cancellationToken);

            var expectedStrings = new[] { "A obra não contém referências." };

            result.Should().BeEquivalentTo(expectedStrings);
        }
    }
}
