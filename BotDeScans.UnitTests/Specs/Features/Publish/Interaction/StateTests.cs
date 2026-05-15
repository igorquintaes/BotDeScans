using BotDeScans.App.Features.Publish.Interaction;
using BotDeScans.App.Features.Publish.Interaction.Models;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class StateTests : UnitTest
{
    private readonly State state;

    public StateTests()
    {
        state = new State
        {
            InternalData = new InternalData
            {
                OriginContentFolder = "folder",
                CoverFilePath = "cover.png",
                ZipFilePath = "file.zip",
                PdfFilePath = "file.pdf",
                BloggerImageAsBase64 = "base64",
                BoxPdfReaderKey = "key",
                Pings = "@everyone"
            },
            ReleaseLinks = new Links
            {
                MegaZip = "mega-zip",
                MegaPdf = "mega-pdf",
                DriveZip = "drive-zip",
                DrivePdf = "drive-pdf",
                BoxZip = "box-zip",
                BoxPdf = "box-pdf",
                MangaDex = "mangadex",
                SakuraMangas = "sakura",
                Blogger = "blogger"
            }
        };
    }

    public class Getters : StateTests
    {
        [Fact]
        public void OriginContentFolderShouldReturnInternalDataValue()
            => state.OriginContentFolder.Should().Be("folder");

        [Fact]
        public void CoverFilePathShouldReturnInternalDataValue()
            => state.CoverFilePath.Should().Be("cover.png");

        [Fact]
        public void ZipFilePathShouldReturnInternalDataValue()
            => state.ZipFilePath.Should().Be("file.zip");

        [Fact]
        public void PdfFilePathShouldReturnInternalDataValue()
            => state.PdfFilePath.Should().Be("file.pdf");

        [Fact]
        public void BloggerImageAsBase64ShouldReturnInternalDataValue()
            => state.BloggerImageAsBase64.Should().Be("base64");

        [Fact]
        public void BoxPdfReaderKeyShouldReturnInternalDataValue()
            => state.BoxPdfReaderKey.Should().Be("key");

        [Fact]
        public void PingsShouldReturnInternalDataValue()
            => state.Pings.Should().Be("@everyone");

        [Fact]
        public void MegaZipLinkShouldReturnReleaseLinksValue()
            => state.MegaZipLink.Should().Be("mega-zip");

        [Fact]
        public void MegaPdfLinkShouldReturnReleaseLinksValue()
            => state.MegaPdfLink.Should().Be("mega-pdf");

        [Fact]
        public void DriveZipLinkShouldReturnReleaseLinksValue()
            => state.DriveZipLink.Should().Be("drive-zip");

        [Fact]
        public void DrivePdfLinkShouldReturnReleaseLinksValue()
            => state.DrivePdfLink.Should().Be("drive-pdf");

        [Fact]
        public void BoxZipLinkShouldReturnReleaseLinksValue()
            => state.BoxZipLink.Should().Be("box-zip");

        [Fact]
        public void BoxPdfLinkShouldReturnReleaseLinksValue()
            => state.BoxPdfLink.Should().Be("box-pdf");

        [Fact]
        public void MangaDexLinkShouldReturnReleaseLinksValue()
            => state.MangaDexLink.Should().Be("mangadex");

        [Fact]
        public void SakuraMangasLinkShouldReturnReleaseLinksValue()
            => state.SakuraMangasLink.Should().Be("sakura");

        [Fact]
        public void BloggerLinkShouldReturnReleaseLinksValue()
            => state.BloggerLink.Should().Be("blogger");
    }

    public class WithMethods : StateTests
    {
        [Fact]
        public void WithOriginContentFolderShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithOriginContentFolder("new-folder");
            newState.InternalData.OriginContentFolder.Should().Be("new-folder");
            state.InternalData.OriginContentFolder.Should().Be("folder");
        }

        [Fact]
        public void WithCoverFilePathShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithCoverFilePath("new-cover.png");
            newState.InternalData.CoverFilePath.Should().Be("new-cover.png");
            state.InternalData.CoverFilePath.Should().Be("cover.png");
        }

        [Fact]
        public void WithZipPathShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithZipPath("new.zip");
            newState.InternalData.ZipFilePath.Should().Be("new.zip");
            state.InternalData.ZipFilePath.Should().Be("file.zip");
        }

        [Fact]
        public void WithPdfPathShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithPdfPath("new.pdf");
            newState.InternalData.PdfFilePath.Should().Be("new.pdf");
            state.InternalData.PdfFilePath.Should().Be("file.pdf");
        }

        [Fact]
        public void WithBloggerImageAsBase64ShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithBloggerImageAsBase64("img64");
            newState.InternalData.BloggerImageAsBase64.Should().Be("img64");
            state.InternalData.BloggerImageAsBase64.Should().Be("base64");
        }

        [Fact]
        public void WithBoxPdfReaderKeyShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithBoxPdfReaderKey("rkey");
            newState.InternalData.BoxPdfReaderKey.Should().Be("rkey");
            state.InternalData.BoxPdfReaderKey.Should().Be("key");
        }

        [Fact]
        public void WithPingsShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithPings("@here");
            newState.InternalData.Pings.Should().Be("@here");
            state.InternalData.Pings.Should().Be("@everyone");
        }

        [Fact]
        public void WithMegaZipLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithMegaZipLink("new-link");
            newState.ReleaseLinks.MegaZip.Should().Be("new-link");
            state.ReleaseLinks.MegaZip.Should().Be("mega-zip");
        }

        [Fact]
        public void WithMegaPdfLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithMegaPdfLink("new-link");
            newState.ReleaseLinks.MegaPdf.Should().Be("new-link");
            state.ReleaseLinks.MegaPdf.Should().Be("mega-pdf");
        }

        [Fact]
        public void WithDriveZipLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithDriveZipLink("new-link");
            newState.ReleaseLinks.DriveZip.Should().Be("new-link");
            state.ReleaseLinks.DriveZip.Should().Be("drive-zip");
        }

        [Fact]
        public void WithDrivePdfLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithDrivePdfLink("new-link");
            newState.ReleaseLinks.DrivePdf.Should().Be("new-link");
            state.ReleaseLinks.DrivePdf.Should().Be("drive-pdf");
        }

        [Fact]
        public void WithBoxZipLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithBoxZipLink("new-link");
            newState.ReleaseLinks.BoxZip.Should().Be("new-link");
            state.ReleaseLinks.BoxZip.Should().Be("box-zip");
        }

        [Fact]
        public void WithBoxPdfLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithBoxPdfLink("new-link");
            newState.ReleaseLinks.BoxPdf.Should().Be("new-link");
            state.ReleaseLinks.BoxPdf.Should().Be("box-pdf");
        }

        [Fact]
        public void WithMangaDexLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithMangaDexLink("new-link");
            newState.ReleaseLinks.MangaDex.Should().Be("new-link");
            state.ReleaseLinks.MangaDex.Should().Be("mangadex");
        }

        [Fact]
        public void WithSakuraMangasLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithSakuraMangasLink("new-link");
            newState.ReleaseLinks.SakuraMangas.Should().Be("new-link");
            state.ReleaseLinks.SakuraMangas.Should().Be("sakura");
        }

        [Fact]
        public void WithBloggerLinkShouldReturnNewStateWithUpdatedValue()
        {
            var newState = state.WithBloggerLink("new-link");
            newState.ReleaseLinks.Blogger.Should().Be("new-link");
            state.ReleaseLinks.Blogger.Should().Be("blogger");
        }
    }
}
