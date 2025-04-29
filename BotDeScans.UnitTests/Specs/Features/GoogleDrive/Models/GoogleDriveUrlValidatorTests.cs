using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Features.GoogleDrive.Models;
using FluentResults;
using FluentValidation;
using FluentValidation.TestHelper;
using File = Google.Apis.Drive.v3.Data.File;
namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive.Models;

public class GoogleDriveUrlValidatorTests : UnitTest
{
    const string VALID_URL = "https://drive.google.com/drive/folders/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn";

    public GoogleDriveUrlValidatorTests()
    {
        fixture.FreezeFake<GoogleDriveFilesService>();
        fixture.FreezeFake<IValidator<IList<File>>>();
    }

    [Theory]
    [InlineData("https://drive.google.com/drive/folders/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn?usp=sharing")]
    [InlineData("https://drive.google.com/drive/folders/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
    [InlineData("https://drive.google.com/folderview?id=1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
    [InlineData("https://drive.google.com/open?id=1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
    public void GivenValidLinksShouldReturnSuccess(string url) =>
        fixture.Create<GoogleDriveUrlValidator>()
               .TestValidate(new GoogleDriveUrl(url))
               .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not a valid url")]
    [InlineData("https://random.drive.google.com/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
    [InlineData("https://drive.google.com.random/1LXGFGlcqbdUbdnU8C4aSvmnb5x8AldCn")]
    [InlineData("https://drive.google.com/drive/folders/")]
    [InlineData("https://drive.google.com/drive/folders/randomValue")]
    [InlineData("https://drive.google.com/folderview")]
    [InlineData("https://drive.google.com/folderview?id=")]
    [InlineData("https://drive.google.com/folderview?id=randomValue")]
    public void GivenInvalidLinksShouldReturnError(string url) =>
        fixture.Create<GoogleDriveUrlValidator>()
               .TestValidate(new GoogleDriveUrl(url))
               .ShouldHaveAnyValidationError()
               .WithErrorMessage("O link informado é inválido.");

    [Fact]
    public void GivenErrorToObtainFilesInfoShouldReturnError()
    {
        var data = new GoogleDriveUrl(VALID_URL);

        A.CallTo(() => fixture
               .FreezeFake<GoogleDriveFilesService>()
               .GetManyAsync(data.Id, cancellationToken))
               .Returns(Result.Fail(["err-1", "err-2"]));

        fixture.Create<GoogleDriveUrlValidator>()
               .TestValidate(data)
               .ShouldHaveAnyValidationError()
               .WithErrorMessage("err-1; err-2");

        A.CallTo(() => fixture
               .FreezeFake<IValidator<IList<File>>>()
               .Validate(A<IList<File>>.Ignored))
               .MustNotHaveHappened();
    }

    [Fact]
    public void GivenSuccessValidationShouldCallFilesResultValidation()
    {
        var data = new GoogleDriveUrl(VALID_URL);
        var files = fixture.CreateMany<File>().ToList();

        A.CallTo(() => fixture
               .FreezeFake<GoogleDriveFilesService>()
               .GetManyAsync(data.Id, cancellationToken))
               .Returns(Result.Ok<IList<File>>(files));
    }
}
