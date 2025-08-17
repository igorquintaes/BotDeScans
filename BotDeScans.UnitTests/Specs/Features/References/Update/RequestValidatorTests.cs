using BotDeScans.App.Extensions;
using BotDeScans.App.Features.References.Update;
using BotDeScans.App.Models.Entities;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Features.References.Update;

public abstract class RequestValidatorTests : UnitTest
{
    protected abstract Request Request { get; }

    public class MangaDex : RequestValidatorTests
    {
        protected override Request Request { get; } = 
            new (9, ExternalReference.MangaDex, Guid.NewGuid().ToString());
    }

    public class SakuraMangas : RequestValidatorTests
    {
        protected override Request Request { get; } = 
            new(9, ExternalReference.SakuraMangas, Guid.NewGuid().ToString());
    }

    [Fact]
    public void GivenValidDataShouldNotHaveValidationErrors() =>
        new RequestValidator()
               .TestValidate(Request)
               .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GivenEmptyTitleIdShouldHaveValidationErrors() =>
        new RequestValidator()
               .TestValidate(Request with { TitleId = default })
               .ShouldHaveValidationErrorFor(x => x.TitleId)
               .Only();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GivenEmptyReferenceRawValueShouldHaveValidationErrors(string? value) =>
        new RequestValidator()
               .TestValidate(Request with { ReferenceRawValue = value! })
               .ShouldHaveValidationErrorFor(x => x.ReferenceRawValue)
               .Only();

    [Fact]
    public void GivenUnexpectedReferenceKeyShouldHaveValidationErrors() =>
        new RequestValidator()
                .TestValidate(Request with { ReferenceKey = (ExternalReference)999 })
                .ShouldHaveValidationErrorFor(x => x.ReferenceKey)
                .Only();

    [Theory]
    [InlineData("4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4")]
    [InlineData("https://mangadex.org/title/4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4")]
    [InlineData("https://mangadex.org/title/4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4/extra-path")]
    public void GivenValidMangaDexReferencesShouldNotHaveValidationErrors(string url) =>
        new RequestValidator()
               .TestValidate(Request with { ReferenceRawValue = url })
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("https://otherdomain.org/title/4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4")]
    [InlineData("https://mangadex.org/notitle/4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4")]
    [InlineData("https://mangadex.org/title/invalid-guid-here")]
    [InlineData("https://mangadex.org/title/too-short")]
    public void Should_Return_False_For_Invalid_References(string url) =>
        new RequestValidator()
               .TestValidate(Request with { ReferenceRawValue = url })
               .ShouldHaveValidationErrorFor(x => x.ReferenceRawValue)
               .WithErrorMessage($"Valor de referência inválida para {Request.ReferenceKey.GetDescription()}. " +
                                 $"É necessário o ID da obra ou o link da página da obra.")
               .Only();
}
