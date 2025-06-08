using BotDeScans.App.Features.Publish.Interaction.Pings;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction.Pings;

public class NoneTests : UnitTest
{
    private readonly NonePing ping;

    public NoneTests()
    {
        fixture.FreezeFake<IConfiguration>();

        ping = fixture.Create<NonePing>();
    }

    public class IsApplicable : NoneTests
    {
        [Fact]
        public void GivenExpectedPingTypeShouldReturnTrue()
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.None.ToString());

            ping.IsApplicable.Should().BeTrue();
        }

        [Theory]
        [InlineData(PingType.Everyone)]
        [InlineData(PingType.Global)]
        [InlineData(PingType.Role)]
        [InlineData((PingType)999)]
        public void GivenUnexpectedPingTypeShouldReturnFalse(PingType pingType)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());

            ping.IsApplicable.Should().BeFalse();
        }
    }

    public class GetPingAsTextAsync : NoneTests
    {
        [Fact]
        public async Task ShouldReturnExpectedText()
        {
            var result = await ping.GetPingAsTextAsync(cancellationToken);
            result.Should().Be(string.Empty);
        }
    }
}
