using BotDeScans.App.Features.Titles.List;
using BotDeScans.App.Infra.Repositories;
using BotDeScans.App.Models.Entities;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Extensions.Errors;
using Remora.Rest.Core;

namespace BotDeScans.UnitTests.Specs.Features.Titles.List;

public class CommandsTests : UnitTest
{
    private readonly Commands commands;
    private readonly List<Title> titles = [];

    public CommandsTests()
    {
        titles.Add(fixture.Build<Title>().With(x => x.Name, "AAA").With(x => x.Id, 2).Create());
        titles.Add(fixture.Build<Title>().With(x => x.Name, "BBB").With(x => x.Id, 3).Create());
        titles.Add(fixture.Build<Title>().With(x => x.Name, "CCC").With(x => x.Id, 1).Create());
        
        fixture.FreezeFake<IFeedbackService>();
        fixture.FreezeFake<TitleRepository>();

        commands = fixture.CreateCommand<Commands>(cancellationToken);

        A.CallTo(() => fixture
            .FreezeFake<TitleRepository>()
            .GetTitlesAsync(cancellationToken))
            .Returns(titles);
    }

    [Fact]
    public async Task GivenSuccessExecutionShouldReturnSuccess()
    {
        var result = await commands.List();

        result.IsSuccess.Should().BeTrue();
        AssertWarningMessage(times: 0);
        AssertEmbedMessage(times: 1);
    }

    [Fact]
    public async Task GivenSuccessExecutionShouldCreateSuccessEmbed()
    {
        Embed embedResult = null!;
        A.CallTo(() => fixture
            .FreezeFake<IFeedbackService>()
            .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
            .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

        await commands.List();

        embedResult.Should().NotBeNull();
        AssertWarningMessage(times: 0);
        AssertEmbedMessage(times: 1);

        await Verify(embedResult);
    }

    [Fact]
    public async Task GivenNoTitlesShouldReturnSuccess()
    {
        A.CallTo(() => fixture
            .FreezeFake<TitleRepository>()
            .GetTitlesAsync(cancellationToken))
            .Returns([]);

        var result = await commands.List();

        result.IsSuccess.Should().BeTrue();
        AssertWarningMessage(times: 1);
        AssertEmbedMessage(times: 0);
    }

    [Fact]
    public async Task GivenErrorToCreateInteractionWhenNoTitlesShouldReturnError()
    {
        A.CallTo(() => fixture
            .FreezeFake<TitleRepository>()
            .GetTitlesAsync(cancellationToken))
            .Returns([]);

        A.CallTo(() => fixture
            .FreezeFake<IFeedbackService>()
            .SendContextualWarningAsync(
                 "Não há obras cadastradas.", 
                A<Snowflake?>.Ignored, 
                A<FeedbackMessageOptions>.Ignored, 
                cancellationToken))
            .Returns(Remora.Results.Result<IReadOnlyList<IMessage>>
                .FromError(new ValidationError("prop", "reason")));

        var result = await commands.List();

        result.IsSuccess.Should().BeFalse();
        AssertWarningMessage(times: 1);
        AssertEmbedMessage(times: 0);
    }

    [Fact]
    public async Task GivenErrorToCreateInteractionShouldReturnError()
    {
        A.CallTo(() => fixture
            .FreezeFake<IFeedbackService>()
            .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
            .Returns(Remora.Results.Result<IMessage>.FromError(new ValidationError("prop", "reason")));
        
        var result = await commands.List();

        result.IsSuccess.Should().BeFalse();
        AssertWarningMessage(times: 0);
        AssertEmbedMessage(times: 1);
    }

    private void AssertWarningMessage(int times)
    {
        A.CallTo(() => fixture
            .FreezeFake<IFeedbackService>()
            .SendContextualWarningAsync(
                 "Não há obras cadastradas.",
                A<Snowflake?>.Ignored,
                A<FeedbackMessageOptions>.Ignored,
                cancellationToken))
            .MustHaveHappened(times, Times.Exactly);

        Fake.GetCalls(fixture.FreezeFake<IFeedbackService>())
            .Where(x => x.Method.Name == nameof(IFeedbackService.SendContextualWarningAsync))
            .Should().HaveCount(times);
    }

    private void AssertEmbedMessage(int times)
    {
        A.CallTo(() => fixture
            .FreezeFake<IFeedbackService>()
            .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
            .MustHaveHappened(times, Times.Exactly);

        Fake.GetCalls(fixture.FreezeFake<IFeedbackService>())
            .Where(x => x.Method.Name == nameof(IFeedbackService.SendContextualEmbedAsync))
            .Should().HaveCount(times);
    }
}
