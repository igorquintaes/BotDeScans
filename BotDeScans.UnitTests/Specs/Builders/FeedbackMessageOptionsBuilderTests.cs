using BotDeScans.App.Builders;

namespace BotDeScans.UnitTests.Specs.Builders;

public class FeedbackMessageOptionsBuilderTests : UnitTest
{
    public class WithAttachment : FeedbackMessageOptionsBuilderTests
    {
        [Fact]
        public void GivenMoreThan10AttachmentsShouldThrowException()
        {
            var builder = new FeedbackMessageOptionsBuilder();
            for (int i = 0; i < 11; i++)
            {
                builder.WithAttachment($"file{i}.txt", new MemoryStream());
            }

            Action act = () => builder.WithAttachment("file11.txt", new MemoryStream());
           
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage("Discord allows only 10 attachments for each message. (Parameter 'name')");
        }

        [Fact]
        public void GivenValidAttachmentsShouldBuildExpectedOptions()
        {
            const int MAX_ATTACHMENTS = FeedbackMessageOptionsBuilder.MAX_ATTACHMENTS_ALLOWED;
            var builder = new FeedbackMessageOptionsBuilder();
            for (int i = 0; i < MAX_ATTACHMENTS; i++)
            {
                builder.WithAttachment($"file{i}.txt", new MemoryStream());
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

            options.Attachments.HasValue.Should().BeFalse();
        }

        [Fact]
        public void GivenAttachmentsShouldBuildOptionsWithAttachments()
        {
            var builder = new FeedbackMessageOptionsBuilder()
                .WithAttachment("file1.txt", new MemoryStream())
                .WithAttachment("file2.txt", new MemoryStream());

            var options = builder.Build();

            options.Attachments.Value.Should().HaveCount(2);
            options.Attachments.Value[0].AsT0.Name.Should().Be("file1.txt");
            options.Attachments.Value[1].AsT0.Name.Should().Be("file2.txt");
        }
    }
}
