using BotDeScans.App.Builders;
using FluentAssertions.Execution;
using FluentResults;
using Remora.Discord.API.Objects;
using System.Drawing;

namespace BotDeScans.UnitTests.Specs.Builders;

public class EmbedBuilderTests : UnitTest
{
    public class CreateErrorEmbed : EmbedBuilderTests
    {
        [Fact]
        public void GivenSuccessResultShouldThrowException()
        {
            Action act = () => EmbedBuilder.CreateErrorEmbed(Result.Ok());

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Error embed must be called with an error result state");
        }

        [Fact]
        public void GivenSimpleErrorShouldReturnExpectedEmbed()
        {
            var errorMessage = fixture.Create<string>();
            var result = Result.Fail(errorMessage);

            var embed = EmbedBuilder.CreateErrorEmbed(result);

            using var _ = new AssertionScope();
            embed.Title.Value.Should().Be(":no_entry: Erro!");
            embed.Colour.Value.Should().Be(Color.Red);
            embed.Fields.Value.Should().HaveCount(1);
            embed.Fields.Value[0].Name.Should().Be("Erro 1");
            embed.Fields.Value[0].Value.Should().Be(errorMessage);
        }

        [Fact]
        public void GivenSimpleExceptionalErrorShouldReturnExpectedEmbed()
        {
            var errorMessage = fixture.Create<string>();
            var exceptionMessage = fixture.Create<string>();
            var exception = new InvalidOperationException(exceptionMessage);
            var result = Result.Fail(new Error(errorMessage).CausedBy(exception));

            var embed = EmbedBuilder.CreateErrorEmbed(result);

            using var _ = new AssertionScope();
            embed.Title.Value.Should().Be(":no_entry: Erro!");
            embed.Colour.Value.Should().Be(Color.Red);
            embed.Fields.Value.Should().HaveCount(2);
            embed.Fields.Value[0].Name.Should().Be("Erro 1");
            embed.Fields.Value[0].Value.Should().Be(errorMessage);
            embed.Fields.Value[1].Name.Should().Be(":warning: Detalhe de exceção");
            embed.Fields.Value[1].Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void GivenNestedErrorsShouldReturnExpectedEmbed()
        {
            var innerErrorMessage = fixture.Create<string>();
            var outerErrorMessage = fixture.Create<string>();
            var result = Result
                .Fail(new Error(outerErrorMessage)
                .CausedBy(new Error(innerErrorMessage)));

            var embed = EmbedBuilder.CreateErrorEmbed(result);

            using var _ = new AssertionScope();
            embed.Title.Value.Should().Be(":no_entry: Erro!");
            embed.Colour.Value.Should().Be(Color.Red);
            embed.Fields.Value.Should().HaveCount(2);
            embed.Fields.Value[0].Name.Should().Be("Erro 1");
            embed.Fields.Value[0].Value.Should().Be(outerErrorMessage);
            embed.Fields.Value[1].Name.Should().Be(":arrow_right: Detalhe interno");
            embed.Fields.Value[1].Value.Should().Be(innerErrorMessage);
        }

        [Fact]
        public void GivenComplexNestedErrorsShouldReturnExpectedEmbed()
        {
            var level1Message = fixture.Create<string>();
            var level2Message = fixture.Create<string>();
            var otherLevel2Message = fixture.Create<string>();
            var exceptionMessage = fixture.Create<string>();
            var exception = new InvalidOperationException(exceptionMessage);

            var result = Result
                .Fail(new Error(level1Message)
                    .CausedBy(new Error(level2Message).CausedBy(exception))
                    .CausedBy(new Error(otherLevel2Message)));

            var embed = EmbedBuilder.CreateErrorEmbed(result);

            using var _ = new AssertionScope();
            embed.Title.Value.Should().Be(":no_entry: Erro!");
            embed.Colour.Value.Should().Be(Color.Red);
            embed.Fields.Value.Should().HaveCount(4);
            embed.Fields.Value[0].Name.Should().Be("Erro 1");
            embed.Fields.Value[0].Value.Should().Be(level1Message);
            embed.Fields.Value[1].Name.Should().Be(":arrow_right: Detalhe interno");
            embed.Fields.Value[1].Value.Should().Be(level2Message);
            embed.Fields.Value[2].Name.Should().Be(":warning: Detalhe de exceção");
            embed.Fields.Value[2].Value.Should().Be(exceptionMessage);
            embed.Fields.Value[3].Name.Should().Be(":arrow_right: Detalhe interno");
            embed.Fields.Value[3].Value.Should().Be(otherLevel2Message);
        }
    }

    public class CreateSuccessEmbed : EmbedBuilderTests
    {
        [Fact]
        public void GivenFilledParametersShouldCreateExpectedEmbed()
        {
            var title = fixture.Create<string>();
            var description = fixture.Create<string>();
            var imageUrl = fixture.Create<string>();
            var image = new EmbedImage(Url: imageUrl);

            var embed = EmbedBuilder.CreateSuccessEmbed(title, description, image);

            using var _ = new AssertionScope();
            embed.Title.HasValue.Should().BeTrue();
            embed.Title.Value.Should().Be(title);

            embed.Description.HasValue.Should().BeTrue();
            embed.Description.Value.Should().Be(description);

            embed.Image.HasValue.Should().BeTrue();
            embed.Image.Value.Url.Should().Be(imageUrl);

            embed.Colour.HasValue.Should().BeTrue();
            embed.Colour.Value.Should().Be(Color.Green);
        }

        [Fact]
        public void GivenNoParametersShouldCreateDefaultEmbed()
        {
            var embed = EmbedBuilder.CreateSuccessEmbed();

            using var _ = new AssertionScope();
            embed.Title.HasValue.Should().BeTrue();
            embed.Title.Value.Should().Be("Sucesso!");

            embed.Colour.HasValue.Should().BeTrue();
            embed.Colour.Value.Should().Be(Color.Green);

            embed.Description.HasValue.Should().BeFalse();
            embed.Image.HasValue.Should().BeFalse();
        }
    }
}
