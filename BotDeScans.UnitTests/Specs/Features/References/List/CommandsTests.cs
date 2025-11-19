using BotDeScans.App.Features.References.List;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Extensions.Errors;
using Remora.Results;

namespace BotDeScans.UnitTests.Specs.Features.References.List;

public class CommandsTests : UnitTest
{
    private readonly Commands commands;

    public CommandsTests()
    {
        fixture.FreezeFake<Handler>();
        fixture.FreezeFake<IFeedbackService>();

        commands = fixture.CreateCommand<Commands>(cancellationToken);
    }

    public class ExecuteAsync : CommandsTests
    {
        private static readonly string[] References = ["ref 1", "ref 2"];

        public ExecuteAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<int>.Ignored, cancellationToken))
                .Returns(References);

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(new Result<IMessage>());
        }

        [Fact]
        public async Task GivenValidRequestShouldReturnSuccess()
        {
            var result = await commands.ExecuteAsync(fixture.Create<int>());

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenValidRequestShouldReturnExpectedEmbed()
        {
            Embed embedResult = null!;

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.ExecuteAsync(fixture.Create<int>());

            embedResult.Should().NotBeNull();
            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenErrorRequestShouldReturnSuccess()
        {
            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<int>.Ignored, cancellationToken))
                .Returns(FluentResults.Result.Fail("some error"));

            var result = await commands.ExecuteAsync(fixture.Create<int>());

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenErrorRequestShouldReturnExpectedEmbed()
        {
            Embed embedResult = null!;

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<int>.Ignored, cancellationToken))
                .Returns(FluentResults.Result.Fail("some error"));

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.ExecuteAsync(fixture.Create<int>());

            embedResult.Should().NotBeNull();
            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenErrorToSendFeedbackMessageShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.ExecuteAsync(fixture.Create<int>());

            result.IsSuccess.Should().BeFalse();
        }
    }
}
