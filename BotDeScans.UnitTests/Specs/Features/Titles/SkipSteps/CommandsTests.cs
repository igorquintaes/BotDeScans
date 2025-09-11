using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Extensions.Errors;
using Remora.Results;
using static BotDeScans.App.Features.Titles.SkipSteps.Commands;

namespace BotDeScans.UnitTests.Specs.Features.Titles.SkipSteps;

public abstract class CommandsTests : UnitTest
{
    private readonly ChildCommands commands;

    public CommandsTests()
    {
        fixture.FreezeFake<IFeedbackService>();
        fixture.FreezeFake<App.Features.Titles.SkipSteps.Add.Handler>();
        fixture.FreezeFake<App.Features.Titles.SkipSteps.List.Handler>();
        fixture.FreezeFake<App.Features.Titles.SkipSteps.Remove.Handler>();

        commands = fixture.CreateCommand<ChildCommands>(cancellationToken);
    }

    public class ExecuteAddAsync : CommandsTests
    {
        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var titleId = fixture.Create<int>();
            var step = StepName.UploadMangadex.ToString();

            var result = await commands.ExecuteAddAsync(titleId, step);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenExecutionShouldCallHandlerWithExpectedParams()
        {
            var titleId = fixture.Create<int>();
            var step = StepName.UploadMangadex.ToString();

            await commands.ExecuteAddAsync(titleId, step);

            A.CallTo(() => fixture
                .FreezeFake<App.Features.Titles.SkipSteps.Add.Handler>()
                .ExecuteAsync(titleId, StepName.UploadMangadex, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnExpectedObjectInMessage()
        {
            const int titleId = 4;
            const StepName step = StepName.UploadMangadex; 
            Embed embedResult = null!;

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.ExecuteAddAsync(titleId, step.ToString());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenErrorToSendFeedbackMessageShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.ExecuteAddAsync(
                fixture.Create<int>(),
                StepName.UploadMangadex.ToString());

            result.IsSuccess.Should().BeFalse();
        }
    }

    public class ExecuteRemoveAsync : CommandsTests
    {
        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var titleId = fixture.Create<int>();
            var step = StepName.UploadMangadex.ToString();

            var result = await commands.ExecuteRemoveAsync(titleId, step);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenExecutionShouldCallHandlerWithExpectedParams()
        {
            var titleId = fixture.Create<int>();
            var step = StepName.UploadMangadex.ToString();

            await commands.ExecuteRemoveAsync(titleId, step);

            A.CallTo(() => fixture
                .FreezeFake<App.Features.Titles.SkipSteps.Remove.Handler>()
                .ExecuteAsync(titleId, StepName.UploadMangadex, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnExpectedObjectInMessage()
        {
            const int titleId = 4;
            const StepName step = StepName.UploadMangadex; 
            Embed embedResult = null!;

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.ExecuteRemoveAsync(titleId, step.ToString());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenErrorToSendFeedbackMessageShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.ExecuteRemoveAsync(
                fixture.Create<int>(),
                StepName.UploadMangadex.ToString());

            result.IsSuccess.Should().BeFalse();
        }
    }

    public class ExecuteListAsync : CommandsTests
    {
        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var titleId = fixture.Create<int>();

            var result = await commands.ExecuteListAsync(titleId);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenExecutionShouldCallHandlerWithExpectedParams()
        {
            var titleId = fixture.Create<int>();

            await commands.ExecuteListAsync(titleId);

            A.CallTo(() => fixture
                .FreezeFake<App.Features.Titles.SkipSteps.List.Handler>()
                .ExecuteAsync(titleId, A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnExpectedObjectInMessage()
        {
            const int titleId = 4;
            Embed embedResult = null!;

            A.CallTo(() => fixture
                .FreezeFake<App.Features.Titles.SkipSteps.List.Handler>()
                .ExecuteAsync(titleId, A<CancellationToken>.Ignored))
                .Returns(["item1", "item2"]);

            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.ExecuteListAsync(titleId);

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenErrorToSendFeedbackMessageShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.ExecuteListAsync(fixture.Create<int>());

            result.IsSuccess.Should().BeFalse();
        }
    }
}
