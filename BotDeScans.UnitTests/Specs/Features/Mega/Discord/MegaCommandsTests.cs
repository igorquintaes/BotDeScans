using BotDeScans.App.Features.Mega.Discord;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services;
using BotDeScans.App.Services.Discord;
using CG.Web.MegaApiClient;
using FluentResults;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Extensions.Errors;

namespace BotDeScans.UnitTests.Specs.Features.Mega.Discord;

public class MegaCommandsTests : UnitTest
{
    private readonly MegaCommands commands;

    public MegaCommandsTests()
    {
        fixture.FreezeFake<MegaSettingsService>();
        fixture.FreezeFake<ExtendedFeedbackService>();
        fixture.FreezeFake<ChartService>();

        commands = fixture.CreateCommand<MegaCommands>(cancellationToken);
    }

    public class DataUsage : MegaCommandsTests
    {
        private readonly INode rootNode;

        public DataUsage()
        {
            rootNode = fixture.FreezeFake<INode>();

            A.CallTo(() => rootNode.Id)
                .Returns(fixture.Create<string>());

            A.CallTo(() => fixture
                .FreezeFake<MegaSettingsService>()
                .GetRootFolderAsync())
                .Returns(rootNode);

            A.CallTo(() => fixture
                .FreezeFake<MegaSettingsService>()
                .GetConsumptionDataAsync(A<string>.Ignored))
                .Returns(Result.Ok(fixture.Create<ConsumptionData>()));

            A.CallTo(() => fixture
                .FreezeFake<ChartService>()
                .CreatePieChart(A<ConsumptionData>.Ignored))
                .Returns(fixture.FreezeFake<Stream>());
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var result = await commands.DataUsage();

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldCreateSuccessEmbed()
        {
            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<ExtendedFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.DataUsage();

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenFailedGetConsumptionDataShouldCreateErrorEmbed()
        {
            A.CallTo(() => fixture
                .FreezeFake<MegaSettingsService>()
                .GetConsumptionDataAsync(A<string>.Ignored))
                .Returns(Result.Fail("error message."));

            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<ExtendedFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.DataUsage();

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenFailedGetConsumptionDataShouldStillReturnSuccess()
        {
            A.CallTo(() => fixture
                .FreezeFake<MegaSettingsService>()
                .GetConsumptionDataAsync(A<string>.Ignored))
                .Returns(Result.Fail("error message."));

            var result = await commands.DataUsage();

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task GivenErrorToSendSuccessFeedbackMessageShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<ExtendedFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.DataUsage();

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GivenErrorToSendErrorFeedbackMessageShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<MegaSettingsService>()
                .GetConsumptionDataAsync(A<string>.Ignored))
                .Returns(Result.Fail("error message."));

            A.CallTo(() => fixture
                .FreezeFake<ExtendedFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.DataUsage();

            result.IsSuccess.Should().BeFalse();
        }
    }
}