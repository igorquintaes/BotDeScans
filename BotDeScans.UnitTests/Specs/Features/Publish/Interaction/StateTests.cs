using BotDeScans.App.Features.Publish.Interaction;

namespace BotDeScans.UnitTests.Specs.Features.Publish.Interaction;

public class StateTests : UnitTest
{
    private readonly State state;

    public StateTests()
    {
        state = new State
        {
            OriginContentFolder = "folder",
            CoverFilePath = "cover.png",
            ZipFilePath = "file.zip",
            PdfFilePath = "file.pdf",
            BloggerImageAsBase64 = "base64",
            BoxPdfReaderKey = "key",
            Pings = "@everyone",
            MegaZipLink = "mega-zip",
            MegaPdfLink = "mega-pdf",
            DriveZipLink = "drive-zip",
            DrivePdfLink = "drive-pdf",
            BoxZipLink = "box-zip",
            BoxPdfLink = "box-pdf",
            MangaDexLink = "mangadex",
            SakuraMangasLink = "sakura",
            BloggerLink = "blogger"
        };
    }

    public class ImmutabilityTests : StateTests
    {
        [Fact]
        public void WithShouldReturnNewStateWithUpdatedOriginContentFolder()
        {
            var newState = state with { OriginContentFolder = "new-folder" };
            newState.OriginContentFolder.Should().Be("new-folder");
            state.OriginContentFolder.Should().Be("folder");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedCoverFilePath()
        {
            var newState = state with { CoverFilePath = "new-cover.png" };
            newState.CoverFilePath.Should().Be("new-cover.png");
            state.CoverFilePath.Should().Be("cover.png");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedZipFilePath()
        {
            var newState = state with { ZipFilePath = "new.zip" };
            newState.ZipFilePath.Should().Be("new.zip");
            state.ZipFilePath.Should().Be("file.zip");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedPdfFilePath()
        {
            var newState = state with { PdfFilePath = "new.pdf" };
            newState.PdfFilePath.Should().Be("new.pdf");
            state.PdfFilePath.Should().Be("file.pdf");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedBloggerImageAsBase64()
        {
            var newState = state with { BloggerImageAsBase64 = "img64" };
            newState.BloggerImageAsBase64.Should().Be("img64");
            state.BloggerImageAsBase64.Should().Be("base64");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedBoxPdfReaderKey()
        {
            var newState = state with { BoxPdfReaderKey = "rkey" };
            newState.BoxPdfReaderKey.Should().Be("rkey");
            state.BoxPdfReaderKey.Should().Be("key");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedPings()
        {
            var newState = state with { Pings = "@here" };
            newState.Pings.Should().Be("@here");
            state.Pings.Should().Be("@everyone");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedMegaZipLink()
        {
            var newState = state with { MegaZipLink = "new-link" };
            newState.MegaZipLink.Should().Be("new-link");
            state.MegaZipLink.Should().Be("mega-zip");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedMegaPdfLink()
        {
            var newState = state with { MegaPdfLink = "new-link" };
            newState.MegaPdfLink.Should().Be("new-link");
            state.MegaPdfLink.Should().Be("mega-pdf");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedDriveZipLink()
        {
            var newState = state with { DriveZipLink = "new-link" };
            newState.DriveZipLink.Should().Be("new-link");
            state.DriveZipLink.Should().Be("drive-zip");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedDrivePdfLink()
        {
            var newState = state with { DrivePdfLink = "new-link" };
            newState.DrivePdfLink.Should().Be("new-link");
            state.DrivePdfLink.Should().Be("drive-pdf");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedBoxZipLink()
        {
            var newState = state with { BoxZipLink = "new-link" };
            newState.BoxZipLink.Should().Be("new-link");
            state.BoxZipLink.Should().Be("box-zip");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedBoxPdfLink()
        {
            var newState = state with { BoxPdfLink = "new-link" };
            newState.BoxPdfLink.Should().Be("new-link");
            state.BoxPdfLink.Should().Be("box-pdf");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedMangaDexLink()
        {
            var newState = state with { MangaDexLink = "new-link" };
            newState.MangaDexLink.Should().Be("new-link");
            state.MangaDexLink.Should().Be("mangadex");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedSakuraMangasLink()
        {
            var newState = state with { SakuraMangasLink = "new-link" };
            newState.SakuraMangasLink.Should().Be("new-link");
            state.SakuraMangasLink.Should().Be("sakura");
        }

        [Fact]
        public void WithShouldReturnNewStateWithUpdatedBloggerLink()
        {
            var newState = state with { BloggerLink = "new-link" };
            newState.BloggerLink.Should().Be("new-link");
            state.BloggerLink.Should().Be("blogger");
        }
    }
}
