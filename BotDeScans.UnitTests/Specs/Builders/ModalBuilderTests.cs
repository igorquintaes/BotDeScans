using BotDeScans.App.Builders;
using Remora.Discord.API.Abstractions.Objects;

namespace BotDeScans.UnitTests.Specs.Builders;

public class ModalBuilderTests : UnitTest
{
    public class AddField : ModalBuilderTests
    {
        [Fact]
        public void GivenMoreThan5FieldsShouldThrowException()
        {
            var builder = new ModalBuilder("test_modal", "Test Modal")
                .AddField("field_1", "Test Field 1")
                .AddField("field_2", "Test Field 2")
                .AddField("field_3", "Test Field 3")
                .AddField("field_4", "Test Field 4")
                .AddField("field_5", "Test Field 5");

            Action act = () => builder.AddField("field_6", "Test Field 6");

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("A modal can have at most 5 fields.");
        }

        [Fact]
        public async Task GivenValidFieldShouldAddFieldSuccessfully()
        {
            var builder = new ModalBuilder("some_modal_id", "Some Modal Title")
                .AddField("field_1", "Test Field 1", isRequired: true, style: TextInputStyle.Short, value: "Default Value 1")
                .AddField("field_2", "Test Field 2", isRequired: true, style: TextInputStyle.Paragraph, value: "Default Value 2")
                .AddField("field_3", "Test Field 3", isRequired: false, style: TextInputStyle.Short)
                .AddField("field_4", "Test Field 4", isRequired: false, style: TextInputStyle.Paragraph)
                .AddField("field_5", "Test Field 5");

            await Verify(builder);
        }
    }

    public class Create : ModalBuilderTests
    {
        [Fact]
        public async Task GivenValidModalShouldCreateSuccessfully()
        {
            var modal = new ModalBuilder("some_modal_id", "Some Modal Title")
                .AddField("field_name", "Test Label")
                .Create();

            await Verify(modal.AsT2);
        }
    }

    public class CreateWithState : ModalBuilderTests
    {
        [Fact]
        public async Task GivenValidModalAndStateShouldCreateSuccessfully()
        {
            var modal = new ModalBuilder("some_modal_id", "Some Modal Title")
                .AddField("field_name", "Test Label")
                .CreateWithState("some_state_value");

            await Verify(modal.AsT2);
        }
    }
}
