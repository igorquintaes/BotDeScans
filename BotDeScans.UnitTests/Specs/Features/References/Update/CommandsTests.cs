using BotDeScans.App.Features.References.Update;
using BotDeScans.App.Models.Entities;
using FluentResults;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;

namespace BotDeScans.UnitTests.Specs.Features.References.Update;

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
        public ExecuteAsync()
        {
            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<Request>.Ignored, cancellationToken))
                .Returns(Result.Ok());

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(new Remora.Results.Result<IMessage>());
        }

        [Fact]
        public async Task GivenValidRequestShouldReturnSuccess()
        {
            var result = await commands.ExecuteAsync(
                fixture.Create<int>(), 
                fixture.Create<ExternalReference>(), 
                fixture.Create<string>());

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

            await commands.ExecuteAsync(
                fixture.Create<int>(),
                fixture.Create<ExternalReference>(),
                fixture.Create<string>());

            embedResult.Should().NotBeNull();
            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenHandlerErrorShouldReturnSuccess()
        {
            var titleId = fixture.Create<int>();
            var referenceKey = fixture.Create<ExternalReference>();
            var referenceValue = fixture.Create<string>();

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<Request>.That.Matches(x =>
                        x.TitleId == titleId &&
                        x.ReferenceKey == referenceKey &&
                        x.ReferenceRawValue == referenceValue), 
                    cancellationToken))
                .Returns(Result.Fail("some error."));

            var result = await commands.ExecuteAsync(titleId, referenceKey, referenceValue);
            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<Request>.That.Matches(x =>
                        x.TitleId == titleId &&
                        x.ReferenceKey == referenceKey &&
                        x.ReferenceRawValue == referenceValue),
                    cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenHandlerErrorShouldReturnExpectedEmbed()
        {
            Embed embedResult = null!;

            A.CallTo(() => fixture
                .FreezeFake<Handler>()
                .ExecuteAsync(A<Request>.Ignored, cancellationToken))
                .Returns(Result.Fail("some error."));

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.ExecuteAsync(
                fixture.Create<int>(),
                fixture.Create<ExternalReference>(),
                fixture.Create<string>());

            embedResult.Should().NotBeNull();
            await Verify(embedResult);
        }
    }
}
