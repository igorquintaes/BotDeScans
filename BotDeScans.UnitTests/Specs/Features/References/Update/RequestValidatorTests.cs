using BotDeScans.App.Features.References.Update;
using BotDeScans.App.Models.Entities;
using FluentValidation.TestHelper;

namespace BotDeScans.UnitTests.Specs.Features.References.Update;

public class RequestValidatorTests : UnitTest
{
    private static readonly Request request = new(9, ExternalReference.MangaDex, Guid.NewGuid().ToString());

    [Fact]
    public void GivenValidDataShouldNotHaveValidationErrors() =>
        fixture.Create<RequestValidator>()
               .TestValidate(request)
               .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GivenEmptyTitleIdShouldHaveValidationErrors() =>
        fixture.Create<RequestValidator>()
               .TestValidate(request with { TitleId = default })
               .ShouldHaveValidationErrorFor(x => x.TitleId)
               .Only();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GivenEmptyReferenceRawValueShouldHaveValidationErrors(string? value) =>
        fixture.Create<RequestValidator>()
               .TestValidate(request with { ReferenceRawValue = value! })
               .ShouldHaveValidationErrorFor(x => x.ReferenceRawValue)
               .Only();

    [Fact]
    public void GivenUnexpectedExternalReferenceShouldHaveValidationErrors() =>
        fixture.Create<RequestValidator>()
               .TestValidate(request with { ReferenceKey = (ExternalReference)999 })
               .ShouldHaveValidationErrorFor(x => x.ReferenceKey)
               .Only();

    [Theory]
    [InlineData("4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4")]
    [InlineData("https://mangadex.org/title/4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4")]
    [InlineData("https://mangadex.org/title/4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4/extra-path")]
    public void GivenValidMangaDexReferencesShouldNotHaveValidationErrors(string url) =>
        fixture.Create<RequestValidator>()
               .TestValidate(request with { ReferenceRawValue = url })
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("https://otherdomain.org/title/4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4")]
    [InlineData("https://mangadex.org/notitle/4d6b898f-5f10-4cb3-a2e5-55e8c3ea8ba4")]
    [InlineData("https://mangadex.org/title/invalid-guid-here")]
    [InlineData("https://mangadex.org/title/too-short")]
    public void Should_Return_False_For_Invalid_References(string url) =>
        fixture.Create<RequestValidator>()
               .TestValidate(request with { ReferenceRawValue = url })
               .ShouldHaveValidationErrorFor(x => x.ReferenceRawValue)
               .WithErrorMessage($"Valor de referência inválida para {ExternalReference.MangaDex}. " +
                                 $"É necessário o ID da obra ou o link da página da obra.")
               .Only();
}
