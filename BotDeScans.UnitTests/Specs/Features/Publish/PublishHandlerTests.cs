using AutoFixture;
using BotDeScans.App.Features.Publish;
using BotDeScans.UnitTests.Extensions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace BotDeScans.UnitTests.Specs.Features.Publish;

public class PublishHandlerTests : UnitTest
{
    private readonly PublishHandler handler;

    public PublishHandlerTests()
    {
        fixture.FreezeFake<PublishService>();

        handler = fixture.Create<PublishHandler>();
    }

    [Fact]
    public async Task GivenSuccessFulExecutionShouldReturnSuccessResult()
    {
        var result = await handler.HandleAsync(() => Task.FromResult(Result.Ok()), cancellationToken);

        result.Should().BeSuccess();
    }
}
