using BotDeScans.App.Features.Titles.Create;
using FluentResults;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;

namespace BotDeScans.UnitTests.Specs.Features.Titles.Create;

public class InteractionsTests : UnitTest
{
    private readonly Interactions interactions;

    public InteractionsTests()
    {
        fixture.FreezeFake<Handler>();
        fixture.FreezeFake<IFeedbackService>();

        interactions = fixture.CreateCommand<Interactions>(cancellationToken);
    }

    public class ExecuteAsync : InteractionsTests
    {
        public ExecuteAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(new Remora.Results.Result<IMessage>());
        }

        [Fact]
        public async Task GivenValidRequestShouldReturnSuccess()
        {
            var result = await interactions.ExecuteAsync(fixture.Create<string>(), fixture.Create<string>());
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

            await interactions.ExecuteAsync(fixture.Create<string>(), fixture.Create<string>());

            embedResult.Should().NotBeNull();
            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenHandlerErrorShouldReturnSuccess()
        {
            var name = fixture.Create<string>();
            var role = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(name, role, cancellationToken))
                .Returns(Result.Fail("some error."));

            var result = await interactions.ExecuteAsync(name, role);
            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(name, role, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenHandlerErrorShouldReturnExpectedEmbed()
        {
            Embed embedResult = null!;

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .Returns(Result.Fail("some error."));

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await interactions.ExecuteAsync(fixture.Create<string>(), fixture.Create<string>());

            embedResult.Should().NotBeNull();
            await Verify(embedResult);
        }
    }
}
