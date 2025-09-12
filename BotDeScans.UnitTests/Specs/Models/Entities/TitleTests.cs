using BotDeScans.App.Models.Entities;
using BotDeScans.App.Models.Entities.Enums;

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

    public class AddSkipStep : TitleTests
    {
        [Fact]
        public void GivenNoneSkipStepsShouldAdd()
        {
            var stepName = StepName.UploadMangadex;
            var title = fixture
                .Build<Title>()
                .With(x => x.SkipSteps, [])
                .Create();

            title.AddSkipStep(stepName);

            title.SkipSteps.Should().ContainSingle()
                 .Which.Step.Should().Be(stepName);
        }

        [Fact]
        public void GivenExistingSkipStepShouldNotAddDuplicate()
        {
            var stepName = StepName.UploadMangadex;
            var title = fixture
                .Build<Title>()
                .With(x => x.SkipSteps, [new() { Step = stepName }])
                .Create();

            title.AddSkipStep(stepName);

            title.SkipSteps.Should().ContainSingle()
                 .Which.Step.Should().Be(stepName);
        }
    }

    public class RemoveSkipStep : TitleTests
    {
        [Fact]
        public void GivenExistingSkipStepShouldRemove()
        {
            var stepName = StepName.UploadMangadex;
            var title = fixture
                .Build<Title>()
                .With(x => x.SkipSteps,
                [
                    new() { Step = stepName },
                    new() { Step = (StepName)999 }
                ])
                .Create();

            title.RemoveSkipStep(stepName);
            title.SkipSteps.Should().ContainSingle()
                 .Which.Step.Should().Be((StepName)999);
        }

        [Fact]
        public void GivenNonExistingSkipStepShouldDoNothing()
        {
            var stepName = StepName.UploadMangadex;
            var title = fixture
                .Build<Title>()
                .With(x => x.SkipSteps,
                [
                    new() { Step = (StepName)998 },
                    new() { Step = (StepName)999 }
                ])
                .Create();
            title.RemoveSkipStep(stepName);
            title.SkipSteps.Should().HaveCount(2);
        }
    }
}