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
    public async Task GivenValidLinksShouldReturnSuccess(string url)
    {
        var result = await fixture
              .Create<GoogleDriveUrlValidator>()
              .TestValidateAsync(new GoogleDriveUrl(url), default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();
    }

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
    public async Task GivenInvalidLinksShouldReturnError(string url)
    {
        var result = await fixture
              .Create<GoogleDriveUrlValidator>()
              .TestValidateAsync(new GoogleDriveUrl(url), default, cancellationToken);

        result.ShouldHaveAnyValidationError()
              .WithErrorMessage("O link informado é inválido.");
    }

    [Fact]
    public async Task GivenErrorToObtainFilesInfoShouldReturnError()
    {
        var data = new GoogleDriveUrl(VALID_URL);

        A.CallTo(() => fixture
              .FreezeFake<GoogleDriveFilesService>()
              .GetManyAsync(data.Id, cancellationToken))
              .Returns(Result.Fail(["err-1", "err-2"]));

        var result = await fixture
              .Create<GoogleDriveUrlValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        result.ShouldHaveAnyValidationError()
              .WithErrorMessage("err-1; err-2");

        A.CallTo(() => fixture
              .FreezeFake<IValidator<IList<File>>>()
              .Validate(A<IList<File>>.Ignored))
              .MustNotHaveHappened();
    }

    [Fact]
    public async Task GivenSuccessValidationShouldCallFilesResultValidation()
    {
        var data = new GoogleDriveUrl(VALID_URL);
        var files = fixture.CreateMany<File>().ToList();

        A.CallTo(() => fixture
               .FreezeFake<GoogleDriveFilesService>()
               .GetManyAsync(data.Id, cancellationToken))
               .Returns(Result.Ok<IList<File>>(files));

        var result = await fixture
              .Create<GoogleDriveUrlValidator>()
              .TestValidateAsync(data, default, cancellationToken);

        result.ShouldNotHaveAnyValidationErrors();

        A.CallTo(() => fixture
               .FreezeFake<GoogleDriveFilesService>()
               .GetManyAsync(data.Id, cancellationToken))
               .MustHaveHappenedOnceExactly();
    }
}
