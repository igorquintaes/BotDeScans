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
            InternalData = new InternalData(),
            ReleaseLinks = new Links()
        };
    }

    public class Getters : StateTests
    {
        [Fact]
        public void OriginContentFolderShouldReturnInternalDataValue()
        {
            state.InternalData.OriginContentFolder = "folder";
            state.OriginContentFolder.Should().Be("folder");
        }

        [Fact]
        public void CoverFilePathShouldReturnInternalDataValue()
        {
            state.InternalData.CoverFilePath = "cover.png";
            state.CoverFilePath.Should().Be("cover.png");
        }

        [Fact]
        public void ZipFilePathShouldReturnInternalDataValue()
        {
            state.InternalData.ZipFilePath = "file.zip";
            state.ZipFilePath.Should().Be("file.zip");
        }

        [Fact]
        public void PdfFilePathShouldReturnInternalDataValue()
        {
            state.InternalData.PdfFilePath = "file.pdf";
            state.PdfFilePath.Should().Be("file.pdf");
        }

        [Fact]
        public void BloggerImageAsBase64ShouldReturnInternalDataValue()
        {
            state.InternalData.BloggerImageAsBase64 = "base64";
            state.BloggerImageAsBase64.Should().Be("base64");
        }

        [Fact]
        public void BoxPdfReaderKeyShouldReturnInternalDataValue()
        {
            state.InternalData.BoxPdfReaderKey = "key";
            state.BoxPdfReaderKey.Should().Be("key");
        }

        [Fact]
        public void PingsShouldReturnInternalDataValue()
        {
            state.InternalData.Pings = "@everyone";
            state.Pings.Should().Be("@everyone");
        }

        [Fact]
        public void MegaZipLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.MegaZip = "link";
            state.MegaZipLink.Should().Be("link");
        }

        [Fact]
        public void MegaPdfLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.MegaPdf = "link";
            state.MegaPdfLink.Should().Be("link");
        }

        [Fact]
        public void DriveZipLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.DriveZip = "link";
            state.DriveZipLink.Should().Be("link");
        }

        [Fact]
        public void DrivePdfLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.DrivePdf = "link";
            state.DrivePdfLink.Should().Be("link");
        }

        [Fact]
        public void BoxZipLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.BoxZip = "link";
            state.BoxZipLink.Should().Be("link");
        }

        [Fact]
        public void BoxPdfLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.BoxPdf = "link";
            state.BoxPdfLink.Should().Be("link");
        }

        [Fact]
        public void MangaDexLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.MangaDex = "link";
            state.MangaDexLink.Should().Be("link");
        }

        [Fact]
        public void SakuraMangasLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.SakuraMangas = "link";
            state.SakuraMangasLink.Should().Be("link");
        }

        [Fact]
        public void BloggerLinkShouldReturnReleaseLinksValue()
        {
            state.ReleaseLinks.Blogger = "link";
            state.BloggerLink.Should().Be("link");
        }
    }

    public class Setters : StateTests
    {
        [Fact]
        public void SetOriginContentFolderShouldUpdateInternalData()
        {
            state.SetOriginContentFolder("new-folder");
            state.InternalData.OriginContentFolder.Should().Be("new-folder");
        }

        [Fact]
        public void SetCoverFilePathShouldUpdateInternalData()
        {
            state.SetCoverFilePath("new-cover.png");
            state.InternalData.CoverFilePath.Should().Be("new-cover.png");
        }

        [Fact]
        public void SetZipPathShouldUpdateInternalData()
        {
            state.SetZipPath("new.zip");
            state.InternalData.ZipFilePath.Should().Be("new.zip");
        }

        [Fact]
        public void SetPdfPathShouldUpdateInternalData()
        {
            state.SetPdfPath("new.pdf");
            state.InternalData.PdfFilePath.Should().Be("new.pdf");
        }

        [Fact]
        public void SetBloggerImageAsBase64ShouldUpdateInternalData()
        {
            state.SetBloggerImageAsBase64("img64");
            state.InternalData.BloggerImageAsBase64.Should().Be("img64");
        }

        [Fact]
        public void SetBoxPdfReaderKeyShouldUpdateInternalData()
        {
            state.SetBoxPdfReaderKey("rkey");
            state.InternalData.BoxPdfReaderKey.Should().Be("rkey");
        }

        [Fact]
        public void SetPingsShouldUpdateInternalData()
        {
            state.SetPings("@here");
            state.InternalData.Pings.Should().Be("@here");
        }

        [Fact]
        public void SetMegaZipLinkShouldUpdateReleaseLinks()
        {
            state.SetMegaZipLink("mega-zip");
            state.ReleaseLinks.MegaZip.Should().Be("mega-zip");
        }

        [Fact]
        public void SetMegaPdfLinkShouldUpdateReleaseLinks()
        {
            state.SetMegaPdfLink("mega-pdf");
            state.ReleaseLinks.MegaPdf.Should().Be("mega-pdf");
        }

        [Fact]
        public void SetDriveZipLinkShouldUpdateReleaseLinks()
        {
            state.SetDriveZipLink("drive-zip");
            state.ReleaseLinks.DriveZip.Should().Be("drive-zip");
        }

        [Fact]
        public void SetDrivePdfLinkShouldUpdateReleaseLinks()
        {
            state.SetDrivePdfLink("drive-pdf");
            state.ReleaseLinks.DrivePdf.Should().Be("drive-pdf");
        }

        [Fact]
        public void SetBoxZipLinkShouldUpdateReleaseLinks()
        {
            state.SetBoxZipLink("box-zip");
            state.ReleaseLinks.BoxZip.Should().Be("box-zip");
        }

        [Fact]
        public void SetBoxPdfLinkShouldUpdateReleaseLinks()
        {
            state.SetBoxPdfLink("box-pdf");
            state.ReleaseLinks.BoxPdf.Should().Be("box-pdf");
        }

        [Fact]
        public void SetMangaDexLinkShouldUpdateReleaseLinks()
        {
            state.SetMangaDexLink("mangadex");
            state.ReleaseLinks.MangaDex.Should().Be("mangadex");
        }

        [Fact]
        public void SetSakuraMangasLinkShouldUpdateReleaseLinks()
        {
            state.SetSakuraMangasLink("sakura");
            state.ReleaseLinks.SakuraMangas.Should().Be("sakura");
        }

        [Fact]
        public void SetBloggerLinkShouldUpdateReleaseLinks()
        {
            state.SetBloggerLink("blogger");
            state.ReleaseLinks.Blogger.Should().Be("blogger");
        }
    }
}
