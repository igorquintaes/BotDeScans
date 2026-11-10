using BotDeScans.App.Services.Discord;
using Remora.Discord.API.Abstractions.Gateway.Bidirectional;
using Remora.Discord.API.Gateway.Bidirectional;
using System.Text.Json;

namespace BotDeScans.Tests.Specs.Unit.Services.Discord;

public class NullSafeHeartbeatConverterTests : UnitTest
{
    private readonly NullSafeHeartbeatConverter converter;

    public NullSafeHeartbeatConverterTests()
    {
        converter = new NullSafeHeartbeatConverter();
    }

    public class HandleNull : NullSafeHeartbeatConverterTests
    {
        [Fact]
        public void ShouldReturnTrue()
        {
            converter.HandleNull.Should().BeTrue();
        }
    }

    public class Read : NullSafeHeartbeatConverterTests
    {
        [Fact]
        public void GivenNullTokenShouldReturnHeartbeatWithNullSequence()
        {
            var json = "null";
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));

            reader.Read();

            var result = converter.Read(ref reader, typeof(IHeartbeat), new JsonSerializerOptions());

            result.Should().NotBeNull();
            result.Should().BeOfType<Heartbeat>();
            result!.LastSequenceNumber.Should().BeNull();
        }

        [Theory]
        [InlineData("123", 123)]
        [InlineData("0", 0)]
        [InlineData("-456", -456)]
        [InlineData("9223372036854775807", long.MaxValue)]
        public void GivenNumberTokenShouldReturnHeartbeatWithSequenceNumber(string json, long expectedSequence)
        {
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));

            reader.Read();

            var result = converter.Read(ref reader, typeof(IHeartbeat), new JsonSerializerOptions());

            result.Should().NotBeNull();
            result.Should().BeOfType<Heartbeat>();
            result!.LastSequenceNumber.Should().Be(expectedSequence);
        }

        [Theory]
        [InlineData("\"invalid\"")]
        [InlineData("{}")]
        [InlineData("[]")]
        [InlineData("true")]
        public void GivenInvalidTokenShouldThrowJsonException(string json)
        {
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            void InvokingConvert()
            {
                var localReader = new Utf8JsonReader(jsonBytes);
                converter.Read(ref localReader, typeof(IHeartbeat), new JsonSerializerOptions());
            }

            Action act = InvokingConvert;
            act.Should().Throw<JsonException>();
        }
    }

    public class Write : NullSafeHeartbeatConverterTests
    {
        [Fact]
        public void GivenNullLastSequenceNumberShouldWriteNullValue()
        {
            var heartbeat = A.Fake<IHeartbeat>();
            A.CallTo(() => heartbeat.LastSequenceNumber).Returns(null);

            var buffer = new MemoryStream();
            var writer = new Utf8JsonWriter(buffer);

            converter.Write(writer, heartbeat, new JsonSerializerOptions());

            writer.Flush();
            var json = System.Text.Encoding.UTF8.GetString(buffer.ToArray());

            json.Should().Be("null");
        }

        [Fact]
        public void GivenNullHeartbeatShouldWriteNullValue()
        {
            IHeartbeat? heartbeat = null;

            var buffer = new MemoryStream();
            var writer = new Utf8JsonWriter(buffer);

            converter.Write(writer, heartbeat, new JsonSerializerOptions());

            writer.Flush();
            var json = System.Text.Encoding.UTF8.GetString(buffer.ToArray());

            json.Should().Be("null");
        }

        [Theory]
        [InlineData(123)]
        [InlineData(0)]
        [InlineData(-456)]
        [InlineData(long.MaxValue)]
        public void GivenValidLastSequenceNumberShouldWriteNumberValue(long sequenceNumber)
        {
            var heartbeat = A.Fake<IHeartbeat>();
            A.CallTo(() => heartbeat.LastSequenceNumber).Returns(sequenceNumber);

            var buffer = new MemoryStream();
            var writer = new Utf8JsonWriter(buffer);

            converter.Write(writer, heartbeat, new JsonSerializerOptions());

            writer.Flush();
            var json = System.Text.Encoding.UTF8.GetString(buffer.ToArray());

            json.Should().Be(sequenceNumber.ToString());
        }
    }
}
