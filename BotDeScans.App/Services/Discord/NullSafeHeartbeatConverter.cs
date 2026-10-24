using Remora.Discord.API.Abstractions.Gateway.Bidirectional;
using Remora.Discord.API.Gateway.Bidirectional;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotDeScans.App.Services.Discord;

// Discord may request a heartbeat (op 1) sending a null payload ("d": null).
// System.Text.Json short-circuits null tokens before reaching Remora's own heartbeat converter,
// so the payload is deserialized as null and the gateway connection is torn down with a JsonException.
// Handling null explicitly keeps the gateway alive during long running operations, such as publish.
public class NullSafeHeartbeatConverter : JsonConverter<IHeartbeat?>
{
    public override bool HandleNull => true;

    public override IHeartbeat? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => new Heartbeat(null),
            JsonTokenType.Number => new Heartbeat(reader.GetInt64()),
            _ => throw new JsonException()
        };

    public override void Write(
        Utf8JsonWriter writer,
        IHeartbeat? value,
        JsonSerializerOptions options)
    {
        if (value?.LastSequenceNumber is null)
            writer.WriteNullValue();
        else
            writer.WriteNumberValue(value.LastSequenceNumber.Value);
    }
}
