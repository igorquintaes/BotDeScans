//using BotDeScans.App.Models;
//using BotDeScans.App.Services.Factories;
//using BotDeScans.App.Services.GoogleDrive;
//using BotDeScans.App.Wrappers;
//using FakeItEasy;
//using FluentAssertions;
//using FluentAssertions.Execution;
//using FluentResults;
//using FluentResults.Extensions.FluentAssertions;
//using Google.Apis.Drive.v3;
//using Google.Apis.Drive.v3.Data;
//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Xunit;
//using static Google.Apis.Drive.v3.AboutResource;

//namespace BotDeScans.UnitTests.Specs.Services.GoogleDrive
//{
//    public class GoogleDriveSettingsServiceTests : UnitTest<GoogleDriveSettingsService>
//    {
//        private readonly DriveService driveService;
//        private readonly GoogleDriveWrapper googleDriveWrapper;
//        private readonly GoogleDriveFoldersService googleDriveFoldersService;

//        public GoogleDriveSettingsServiceTests()
//        {
//            var storageFactory = A.Fake<ExternalServicesFactory>();
//            driveService = A.Fake<DriveService>();
//            googleDriveWrapper = A.Fake<GoogleDriveWrapper>();
//            googleDriveFoldersService = A.Fake<GoogleDriveFoldersService>();

//            A.CallTo(() => storageFactory
//                .CreateGoogleClients())
//                .Returns(Result.Ok(driveService));

//            instance = new (storageFactory, googleDriveWrapper, googleDriveFoldersService);
//        }

//        public class SetUpBaseFolderAsync : GoogleDriveSettingsServiceTests
//        {
//            private const string baseGoogleDriveFolderId = "root";
//            private readonly string folderId;

//            public SetUpBaseFolderAsync()
//            {
//                folderId = dataGenerator.Random.Word();

//                A.CallTo(() => googleDriveFoldersService
//                    .GetFolderAsync(
//                          GoogleDriveSettingsService.BaseFolderName,
//                          baseGoogleDriveFolderId,
//                          cancellationToken))
//                    .Returns(null as File);

//                A.CallTo(() => googleDriveFoldersService
//                    .CreateFolderAsync(
//                          GoogleDriveSettingsService.BaseFolderName,
//                          baseGoogleDriveFolderId,
//                          cancellationToken))
//                    .Returns(new File { Id = folderId });
//            }

//            [Fact]
//            public async Task ShouldCreateBotFolderIfItDoesNotExists()
//            {
//                var result = await instance.SetUpBaseFolderAsync();

//                using (new AssertionScope())
//                {
//                    result.Should().BeSuccess();
//                    GoogleDriveSettingsService.BaseFolderId.Should().Be(folderId);
//                }
//            }

//            [Fact]
//            public async Task ShouldNotCreateBotFolderWhenItExists()
//            {
//                A.CallTo(() => googleDriveFoldersService
//                    .GetFolderAsync(
//                          GoogleDriveSettingsService.BaseFolderName,
//                          baseGoogleDriveFolderId,
//                          cancellationToken))
//                    .Returns(new File { Id = folderId });

//                var result = await instance.SetUpBaseFolderAsync();
//                result.Should().BeSuccess();

//                A.CallTo(() => googleDriveFoldersService
//                    .CreateFolderAsync(
//                          A<string>.Ignored,
//                          A<string>.Ignored,
//                          A<CancellationToken>.Ignored))
//                    .MustNotHaveHappened();
//            }

//            [Fact]
//            public async Task ShouldGetBotFolderIfItAlreadyExists()
//            {
//                A.CallTo(() => googleDriveFoldersService
//                    .GetFolderAsync(
//                          GoogleDriveSettingsService.BaseFolderName,
//                          baseGoogleDriveFolderId,
//                          cancellationToken))
//                    .Returns(new File { Id = folderId });

//                var result = await instance.SetUpBaseFolderAsync();

//                using (new AssertionScope())
//                {
//                    result.Should().BeSuccess();
//                    GoogleDriveSettingsService.BaseFolderId.Should().Be(folderId);
//                }
//            }

//            [Fact]
//            public async Task ShouldRepassGetFolderAsyncError()
//            {
//                var expectedResult = Result.Fail("some error");
//                A.CallTo(() => googleDriveFoldersService
//                    .GetFolderAsync(
//                          GoogleDriveSettingsService.BaseFolderName,
//                          baseGoogleDriveFolderId,
//                          cancellationToken))
//                    .Returns(expectedResult);

//                object result = await instance.SetUpBaseFolderAsync();
//                result.Should().BeEquivalentTo(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldRepassCreateFolderAsyncError()
//            {
//                var expectedResult = Result.Fail("some error");
//                A.CallTo(() => googleDriveFoldersService
//                    .CreateFolderAsync(
//                          GoogleDriveSettingsService.BaseFolderName,
//                          baseGoogleDriveFolderId,
//                          cancellationToken))
//                    .Returns(expectedResult);

//                object result = await instance.SetUpBaseFolderAsync();
//                result.Should().BeEquivalentTo(expectedResult);
//            }

//            [Fact]
//            public void ShouldThrowsAnExceptionWhenTryingToGetBaseFolderIdBeforeSetUpBaseFolder()
//            {
//                Func<string> action = () => GoogleDriveSettingsService.BaseFolderId;
//                action.Should()
//                    .ThrowExactly<InvalidOperationException>()
//                    .WithMessage("Base folder not set.");
//            }
//        }

//        public class GetConsumptionData : GoogleDriveSettingsServiceTests
//        {
//            private readonly AboutResource aboutResource;
//            private readonly GetRequest aboutRequest;
//            private readonly long usedSpace;
//            private readonly long limitSpace;

//            public GetConsumptionData()
//            {
//                aboutResource = A.Fake<AboutResource>();
//                aboutRequest = A.Fake<GetRequest>();
//                usedSpace = dataGenerator.Random.Long(0);
//                limitSpace = dataGenerator.Random.Long(usedSpace);

//                A.CallTo(() => driveService
//                    .About)
//                    .Returns(aboutResource);

//                A.CallTo(() => aboutResource
//                    .Get())
//                    .Returns(aboutRequest);

//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(aboutRequest, cancellationToken))
//                    .Returns(new About
//                    {
//                        StorageQuota = new About.StorageQuotaData
//                        {
//                            Usage = usedSpace,
//                            Limit = limitSpace,
//                        }
//                    });
//            }

//            [Fact]
//            public async Task ShouldCalculateExpectedConsumptionData()
//            {
//                var result = await instance.GetConsumptionData(cancellationToken);

//                using (new AssertionScope())
//                {
//                    result.Should().BeSuccess();
//                    result.ValueOrDefault?.Should().BeEquivalentTo(
//                        new DataUsageDrive(usedSpace, limitSpace - usedSpace));
//                }
//            }

//            [Fact]
//            public async Task ShouldRepassQueryError()
//            {
//                var expectedResult = Result.Fail("some error");
//                A.CallTo(() => googleDriveWrapper
//                    .ExecuteAsync(aboutRequest, cancellationToken))
//                    .Returns(expectedResult);

//                object result = await instance.GetConsumptionData(cancellationToken);
//                result.Should().BeEquivalentTo(expectedResult);
//            }

//            [Fact]
//            public async Task ShouldFillCriteriaAsExpected()
//            {
//                const string EXPECTED_FIELDS = "storageQuota";
//                await instance.GetConsumptionData(cancellationToken);
//                aboutRequest.Fields.Should().Be(EXPECTED_FIELDS);
//            }
//        }
//    }
//}
