using BotDeScans.App.Builders;

namespace BotDeScans.UnitTests.Specs.Builders;

public abstract class FeedbackMessageOptionsBuilderTests : UnitTest
{
    public class WithAttachment : FeedbackMessageOptionsBuilderTests
    {
        [Fact]
        public void GivenMoreThanMaxAttachmentsAllowedShouldThrowException()
        {
            const int MAX_ATTACHMENTS = FeedbackMessageOptionsBuilder.MAX_ATTACHMENTS_ALLOWED;
            using var stream = new MemoryStream();
            var builder = new FeedbackMessageOptionsBuilder();

            for (int i = 0; i < MAX_ATTACHMENTS; i++)
                builder.WithAttachment($"file{i}.txt", stream);

            Action act = () => builder.WithAttachment("file10.txt", stream);
           
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage($"Discord allows only {FeedbackMessageOptionsBuilder.MAX_ATTACHMENTS_ALLOWED} attachments for each message. (Parameter 'name')");
        }

        [Fact]
        public void GivenValidAttachmentsShouldBuildExpectedOptions()
        {
            const int MAX_ATTACHMENTS = FeedbackMessageOptionsBuilder.MAX_ATTACHMENTS_ALLOWED;
            using var stream = new MemoryStream();
            var builder = new FeedbackMessageOptionsBuilder();

            for (int i = 0; i < MAX_ATTACHMENTS; i++)
            {
                builder.WithAttachment($"file{i}.txt", stream);
            }

            var options = builder.Build();
            options.Attachments.Value.Should().HaveCount(MAX_ATTACHMENTS);
            for (int i = 0; i < MAX_ATTACHMENTS; i++)
            {
                var attachment = options.Attachments.Value[i].AsT0;
                attachment.Name.Should().Be($"file{i}.txt");
            }
        }
    }

    public class Build : FeedbackMessageOptionsBuilderTests
    {
        [Fact]
        public void GivenNoAttachmentsShouldBuildOptionsWithNoAttachments()
        {
            var builder = new FeedbackMessageOptionsBuilder();

            var options = builder.Build();

            options.Attachments.Value.Should().BeNull();
        }

        [Fact]
        public void GivenAttachmentsShouldBuildOptionsWithAttachments()
        {
            using var stream1 = new MemoryStream([0]);
            using var stream2 = new MemoryStream([1]);

            var builder = new FeedbackMessageOptionsBuilder()
                .WithAttachment("file1.txt", stream1)
                .WithAttachment("file2.txt", stream2);

            var options = builder.Build();
            options.Attachments.Value.Should().HaveCount(2);
            options.Attachments.Value[0].AsT0.Name.Should().Be("file1.txt");
            options.Attachments.Value[1].AsT0.Name.Should().Be("file2.txt");
            options.Attachments.Value[0].AsT0.Content.Should().BeSameAs(stream1);
            options.Attachments.Value[1].AsT0.Content.Should().BeSameAs(stream2);
        }
    }
}
