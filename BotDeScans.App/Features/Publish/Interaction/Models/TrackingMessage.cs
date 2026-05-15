using Remora.Rest.Core;

namespace BotDeScans.App.Features.Publish.Interaction.Models;

public record TrackingMessage(
    Snowflake AuthorId,
    Snowflake MessageId);
