using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Feedback.Messages;
namespace BotDeScans.App.Builders;

public class FeedbackMessageOptionsBuilder
{
    private const int MAX_ATTACHMENTS_ALLOWED = 10;

    private List<OneOf<FileData, IPartialAttachment>>? attachments;
    private MessageFlags messageFlags = 0x0;

    public FeedbackMessageOptionsBuilder WithAttachment(string name, Stream stream)
    {
        attachments ??= [];

        if (attachments.Count > MAX_ATTACHMENTS_ALLOWED)
            throw new ArgumentOutOfRangeException(nameof(name), "Discord allows only 10 attachments for each message.");

        attachments.Add(OneOf<FileData, IPartialAttachment>.FromT0(new FileData(name, stream)));
        return this;
    }

    public FeedbackMessageOptionsBuilder AsEphemeral()
    {
        messageFlags = MessageFlags.Ephemeral;
        return this;
    }

    public FeedbackMessageOptions Build()
        => new(Attachments: attachments!) { MessageFlags = messageFlags };
}
