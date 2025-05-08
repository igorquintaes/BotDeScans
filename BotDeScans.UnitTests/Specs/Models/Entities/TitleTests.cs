using BotDeScans.App.Models.Entities;

namespace BotDeScans.UnitTests.Specs.Models.Entities;

public class TitleTests : UnitTest
{
    public class AddOrUpdateReference : TitleTests
    {
        [Fact]
        public void GivenNoneReferenceShouldAdd()
        {
            var title = fixture
                .Build<Title>()
                .With(x => x.References, [])
                .Create();

            var newReference = new TitleReference { Key = ExternalReference.MangaDex, Value = "some-value" };
            title.AddOrUpdateReference(newReference.Key, newReference.Value);

            title.References.Should().BeEquivalentTo([newReference]);
        }

        [Fact]
        public void GivenExistingReferenceButWithDifferentKeyShouldAdd()
        {
            var existingReference = new TitleReference { Key = (ExternalReference)999, Value = "random-value" };
            var title = fixture
                .Build<Title>()
                .With(x => x.References, [existingReference])
                .Create();

            var newReference = new TitleReference { Key = ExternalReference.MangaDex, Value = "some-value" };
            title.AddOrUpdateReference(newReference.Key, newReference.Value);

            title.References.Should().BeEquivalentTo([existingReference, newReference]);
        }

        [Fact]
        public void GivenExistingReferenceWithSameKeyShouldUpdate()
        {
            var existingReference = new TitleReference { Key = ExternalReference.MangaDex, Value = "random-value" };
            var title = fixture
                .Build<Title>()
                .With(x => x.References, [existingReference])
                .Create();

            var newReference = new TitleReference { Key = ExternalReference.MangaDex, Value = "some-value" };
            title.AddOrUpdateReference(newReference.Key, newReference.Value);

            title.References.Should().BeEquivalentTo([newReference]);
        }
    }
}
