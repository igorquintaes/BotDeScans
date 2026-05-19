using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Feedback.Messages;

namespace BotDeScans.App.Builders;

public class FeedbackMessageOptionsBuilder
{
    public const int MAX_ATTACHMENTS_ALLOWED = 10;

    public static readonly string maxAttachmentsError = $"Discord allows only {MAX_ATTACHMENTS_ALLOWED} attachments for each message.";

    private readonly List<OneOf<FileData, IPartialAttachment>> attachments = [];

    public FeedbackMessageOptionsBuilder WithAttachment(string name, Stream stream)
    {
        if (attachments.Count >= MAX_ATTACHMENTS_ALLOWED)
            throw new ArgumentOutOfRangeException(nameof(name), maxAttachmentsError);

        var fileData = new FileData(name, stream);
        var fileAttachment = OneOf<FileData, IPartialAttachment>.FromT0(fileData);
        attachments.Add(fileAttachment);

        return this;
    }

    public FeedbackMessageOptions Build()
        => new(Attachments: attachments);
}
