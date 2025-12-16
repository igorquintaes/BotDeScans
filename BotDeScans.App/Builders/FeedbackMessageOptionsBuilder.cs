using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Feedback.Messages;

namespace BotDeScans.App.Builders;

public class FeedbackMessageOptionsBuilder
{
    public const int MAX_ATTACHMENTS_ALLOWED = 10;

    private List<OneOf<FileData, IPartialAttachment>>? attachments;

    public FeedbackMessageOptionsBuilder WithAttachment(string name, Stream stream)
    {
        attachments ??= [];

        if (attachments.Count > MAX_ATTACHMENTS_ALLOWED)
            throw new ArgumentOutOfRangeException(nameof(name), $"Discord allows only {MAX_ATTACHMENTS_ALLOWED} attachments for each message.");

        attachments.Add(OneOf<FileData, IPartialAttachment>.FromT0(new FileData(name, stream)));
        return this;
    }

    public FeedbackMessageOptions Build()
        => new(Attachments: attachments!);
}
