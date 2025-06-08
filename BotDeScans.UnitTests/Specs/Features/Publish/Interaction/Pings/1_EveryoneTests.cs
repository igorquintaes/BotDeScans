using BotDeScans.App.Features.Publish.Interaction.Pings;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Pings;

public class EveryoneTests : UnitTest
{
    private readonly EveryonePing ping;

    public EveryoneTests()
    {
        fixture.FreezeFake<IConfiguration>();

        ping = fixture.Create<EveryonePing>();
    }

    public class IsApplicable : EveryoneTests
    {
        [Fact]
        public void GivenExpectedPingTypeShouldReturnTrue()
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Everyone.ToString());

            ping.IsApplicable.Should().BeTrue();
        }

        [Theory]
        [InlineData(PingType.Global)]
        [InlineData(PingType.None)]
        [InlineData(PingType.Role)]
        [InlineData((PingType)999)]
        public void GivenUnexpectedPingTypeShouldReturnFalse(PingType pingType)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());

            ping.IsApplicable.Should().BeFalse();
        }
    }

    public class GetPingAsTextAsync : EveryoneTests
    {
        [Fact]
        public async Task ShouldReturnExpectedText()
        {
            var result = await ping.GetPingAsTextAsync(cancellationToken);
            result.Should().Be("@everyone");
        }
    }
}
