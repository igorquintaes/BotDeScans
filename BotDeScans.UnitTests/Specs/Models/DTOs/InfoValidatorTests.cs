using BotDeScans.App.Features.GoogleDrive.Models;
using BotDeScans.App.Models.DTOs;
using FluentValidation;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Models.DTOs;

public class InfoValidatorTests : UnitTest
{
    private readonly Info data;

    public InfoValidatorTests()
    {
        fixture.FreezeFake<IValidator<GoogleDriveUrl>>();

        data = fixture.Build<Info>()
            .With(x => x.ChapterName, fixture.StringOfLength(255))
            .With(x => x.ChapterNumber, "10.1")
            .With(x => x.ChapterVolume, "1")
            .Create();
    }

    [Fact]
    public void GivenValidDataShouldReturnSuccess() =>
        fixture.Create<InfoValidator>()
               .TestValidate(data)
               .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GivenLongChapterNameValueShouldReturnError() =>
        fixture.Create<InfoValidator>()
               .TestValidate(data with { ChapterName = fixture.StringOfLength(256) })
               .ShouldHaveValidationErrorFor(x => x.ChapterName)
               .WithErrorMessage("Nome de capítulo muito longo.");

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("10")]
    [InlineData("1.1")]
    [InlineData("10.1")]
    public void GivenChapterNumberMatchingPatternShouldReturnSuccess(string chapterNumber) =>
        fixture.Create<InfoValidator>()
               .TestValidate(data with { ChapterNumber = chapterNumber })
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("-1")]
    [InlineData("01")]
    [InlineData("1A")]
    [InlineData("A1")]
    [InlineData("1.0")]
    [InlineData("1.11")]
    [InlineData("1-1")]
    [InlineData("1,1")]
    [InlineData("1,11")]
    public void GivenChapterNumberNotMatchingPatternShouldReturnError(string chapterNumber) =>
        fixture.Create<InfoValidator>()
               .TestValidate(data with { ChapterNumber = chapterNumber })
               .ShouldHaveValidationErrorFor(x => x.ChapterNumber)
               .WithErrorMessage("Número do capítulo inválido.");

    [Theory]
    [InlineData("invalid")]
    [InlineData("-1")]
    public void GivenChapterVolumeNotMatchingNaturalNumberShouldReturnError(string chapterVolume) =>
        fixture.Create<InfoValidator>()
               .TestValidate(data with { ChapterVolume = chapterVolume })
               .ShouldHaveValidationErrorFor(x => x.ChapterVolume)
               .WithErrorMessage("Volume do capítulo inválido.");

}
