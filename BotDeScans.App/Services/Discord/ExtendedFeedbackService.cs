using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.Diagnostics.CodeAnalysis;
namespace BotDeScans.App.Services.Discord;

[ExcludeFromCodeCoverage(Justification = @"
        This class would not be necessary if FeedbackService (Remora.Discord) could be mockable.
        In order to test Discord API contracts, we can use a real call to a Json server or Wiremock (maybe we can't change remora's discord url).
        Meanwhile, to test unit rules before Discord API call, we need to mock it to keep our tests clean.")]
public class ExtendedFeedbackService(FeedbackService feedbackService)
{
    public virtual Task<Result<IMessage>> SendContextualEmbedAsync(
        Embed embed,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default)
        => feedbackService.SendContextualEmbedAsync(embed, options, ct);

    public virtual Task<Result<IMessage>> SendEmbedAsync(
        Snowflake channel,
        Embed embed,
        FeedbackMessageOptions? options = null,
        CancellationToken ct = default)
        => feedbackService.SendEmbedAsync(channel, embed, options, ct);
}
