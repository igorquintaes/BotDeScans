using BotDeScans.App.Features.Publish;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Models;
using FluentValidation.TestHelper;
using static BotDeScans.App.Features.Publish.PublishState;

namespace BotDeScans.UnitTests.Specs.Features;

public class PublishStateValidatorTests : UnitTest
{
    public class TitleValidatorTests : PublishStateValidatorTests
    {
        public TitleValidatorTests() => 
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Everyone.ToString());

        [Fact]
        public void GivenValidDataShouldReturnSuccess() => 
            fixture.Create<TitleValidator>()
                   .TestValidate(fixture.Create<Title>())
                   .ShouldNotHaveAnyValidationErrors();

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GivenNonePingTypeShouldReturnError(string? pingType)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType);

            fixture.Create<TitleValidator>()
                   .TestValidate(fixture.Create<Title>())
                   .ShouldHaveValidationErrorFor(x => x)
                   .WithErrorMessage("É necessário definir um tipo de ping no arquivo de configuração do Bot de Scans.");
        }

        [Fact]
        public void GivenInvalidPingTypeShouldReturnError()
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, "invalid");

            fixture.Create<TitleValidator>()
                   .TestValidate(fixture.Create<Title>())
                   .ShouldHaveValidationErrorFor(x => x)
                   .WithErrorMessage("Valor inválido para o tipo de ping no arquivo de configuração do Bot de Scans.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void GivenGlobalPingTypeShouldReturnErrorGlobalPingValueIsNotDefines(string? globalPingValue)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, PingType.Global.ToString());
            fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, globalPingValue);

            fixture.Create<TitleValidator>()
                   .TestValidate(fixture.Create<Title>())
                   .ShouldHaveValidationErrorFor(x => x)
                   .WithErrorMessage("É necessário definir um valor para ping global no arquivo de configuração do Bot de Scans.");
        }

        [Theory]
        [InlineData(PingType.Role, 0UL)]
        [InlineData(PingType.Role, null)]
        [InlineData(PingType.Global, 0UL)]
        [InlineData(PingType.Global, null)]
        public void GivenSomePingTypesShouldHaveDiscordRoleIdDefined(PingType pingType, ulong? roleId)
        {
            fixture.FreezeFakeConfiguration(Ping.PING_TYPE_KEY, pingType.ToString());
            fixture.FreezeFakeConfiguration(GlobalPing.GLOBAL_ROLE_KEY, "some-role");

            fixture.Create<TitleValidator>()
                   .TestValidate(fixture.Create<Title>() with { DiscordRoleId = roleId })
                   .ShouldHaveValidationErrorFor(x => x.DiscordRoleId)
                   .WithErrorMessage(
                         $"Não foi definida uma role para o Discord nesta obra, obrigatória para o ping de tipo {pingType}. " +
                         $"Defina, ou mude o tipo de ping para publicação no arquivo de configuração do Bot de Scans.");
        }
    }

    public class InfoValidatorTests : PublishStateValidatorTests
    {
        private readonly Info info;

        public InfoValidatorTests() => 
            info = fixture.Build<Info>()
                .With(x => x.ChapterName, fixture.StringOfLength(255))
                .With(x => x.ChapterNumber, "10")
                .With(x => x.ChapterVolume, "1")
                .Create();

        [Fact]
        public void GivenValidDataShouldReturnSuccess() =>
            fixture.Create<InfoValidator>()
                   .TestValidate(info)
                   .ShouldNotHaveAnyValidationErrors();

        [Fact]
        public void GivenLongChapterNameValueShouldReturnError() => 
            fixture.Create<InfoValidator>()
                   .TestValidate(info with { ChapterName = fixture.StringOfLength(256) })
                   .ShouldHaveValidationErrorFor(x => x.ChapterName)
                   .WithErrorMessage("Nome de capítulo muito longo.");

        [Theory]
        [InlineData("invalid")]
        [InlineData("10invalid")]
        [InlineData("invalid10")]
        public void GivenChapterNumberNotMatchingPatternShouldReturnError(string chapterNumber) => 
            fixture.Create<InfoValidator>()
                   .TestValidate(info with { ChapterNumber = chapterNumber })
                   .ShouldHaveValidationErrorFor(x => x.ChapterNumber)
                   .WithErrorMessage("Número do capítulo inválido.");

        [Theory]
        [InlineData("invalid")]
        [InlineData("-1")]
        public void GivenChapterVolumeNotMatchingNaturalNumberShouldReturnError(string chapterVolume) => 
            fixture.Create<InfoValidator>()
                   .TestValidate(info with { ChapterVolume = chapterVolume })
                   .ShouldHaveValidationErrorFor(x => x.ChapterVolume)
                   .WithErrorMessage("Volume do capítulo inválido.");
    }
}
