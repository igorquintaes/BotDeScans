using BotDeScans.App.Features.References.Update;
using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Features.References.Update;

public class RequestTests : UnitTest
{
    public class ReferenceValue : RequestTests
    {
        [Fact]
        public void GivenRawValueAsGuidShouldReturnExpectedValue()
        {
            const string EXPECTED_RESULT = "801513ba-a712-498c-8f57-cae55b38cc92";

            var result = new Request(fixture.Create<int>(), fixture.Create<ExternalReference>(), EXPECTED_RESULT);

            result.ReferenceValue.Should().Be(EXPECTED_RESULT);
        }

        [Theory]
        [InlineData("https://mangadex.org/title/801513ba-a712-498c-8f57-cae55b38cc92")]
        [InlineData("https://mangadex.org/title/801513ba-a712-498c-8f57-cae55b38cc92/")]
        [InlineData("https://mangadex.org/title/801513ba-a712-498c-8f57-cae55b38cc92/berserk")]
        public void GivenRawValueAsDexLinkShouldReturnExpectedValue(string link)
        {
            const string EXPECTED_RESULT = "801513ba-a712-498c-8f57-cae55b38cc92";

            var result = new Request(fixture.Create<int>(), fixture.Create<ExternalReference>(), link);

            result.ReferenceValue.Should().Be(EXPECTED_RESULT);
        }
    }
}
