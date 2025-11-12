using BotDeScans.App.Features.GoogleDrive;
using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Features.GoogleDrive.Models;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Extensions.Errors;

namespace BotDeScans.UnitTests.Specs.Features.GoogleDrive;

public class CommandsTests : UnitTest
{
    private readonly Commands commands;

    public CommandsTests()
    {
        fixture.FreezeFake<GoogleDriveService>();
        fixture.FreezeFake<GoogleDriveSettingsService>();
        fixture.FreezeFake<ChartService>();
        fixture.FreezeFake<IFeedbackService>();
        fixture.FreezeFake<IValidator<GoogleDriveUrl>>();

        commands = fixture.CreateCommand<Commands>(cancellationToken);
    }

    public class VerifyUrl : CommandsTests
    {
        public VerifyUrl()
        {
            var validationResult = new Fake<ValidationResult>().FakedObject;

            A.CallTo(() => validationResult.IsValid)
                .Returns(true);

            A.CallTo(() => fixture
                .FreezeFake<IValidator<GoogleDriveUrl>>()
                .Validate(A<GoogleDriveUrl>.Ignored))
                .Returns(validationResult);
        }

        [Fact]
        public async Task GivenValidDataShouldCreateSuccessEmbed()
        {
            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.VerifyUrl(fixture.Create<string>());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenInvalidDataShouldCreateErrorEmbed()
        {
            A.CallTo(() => fixture
                .FreezeFake<IValidator<GoogleDriveUrl>>()
                .Validate(A<GoogleDriveUrl>.Ignored))
                .Returns(new ValidationResult(
                    [new ValidationFailure("prop", "reason")]));

            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.VerifyUrl(fixture.Create<string>());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenInvalidDataShouldReturnSuccess()
        {
            var url = fixture.Create<string>();
            var result = await commands.VerifyUrl(url);

                result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<IValidator<GoogleDriveUrl>>()
                .Validate(A<GoogleDriveUrl>.That.Matches(x => x.Url == url)))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToCreateInteractionShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.VerifyUrl(fixture.Create<string>());

            result.IsSuccess.Should().BeFalse();
        }
    }

    public class DeleteFile : CommandsTests
    {
        public DeleteFile()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .DeleteFileByNameAndParentNameAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldCreateSuccessEmbed()
        {
            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.DeleteFile(fixture.Create<string>(), fixture.Create<string>());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GiveFailedExecutionShouldCreateErrorEmbed()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .DeleteFileByNameAndParentNameAsync(A<string>.Ignored, A<string>.Ignored, cancellationToken))
                .Returns(Result.Fail("error message."));

            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.DeleteFile(fixture.Create<string>(), fixture.Create<string>());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var fileName = fixture.Create<string>();
            var folderName = fixture.Create<string>();
            var result = await commands.DeleteFile(fileName, folderName);

            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .DeleteFileByNameAndParentNameAsync(fileName, folderName, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToCreateInteractionShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.DeleteFile(fixture.Create<string>(), fixture.Create<string>());

            result.IsSuccess.Should().BeFalse();
        }
    }

    public class GrantDataAccess : CommandsTests
    {
        public GrantDataAccess()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .GrantReaderAccessToBotFilesAsync(A<string>.Ignored, cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldCreateSuccessEmbed()
        {
            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.GrantDataAccess(fixture.Create<string>());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GiveFailedExecutionShouldCreateErrorEmbed()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .GrantReaderAccessToBotFilesAsync(A<string>.Ignored, cancellationToken))
                .Returns(Result.Fail("error message."));

            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.GrantDataAccess(fixture.Create<string>());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var email = fixture.Create<string>();
            var result = await commands.GrantDataAccess(email);

            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .GrantReaderAccessToBotFilesAsync(email, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToCreateInteractionShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.GrantDataAccess(fixture.Create<string>());

            result.IsSuccess.Should().BeFalse();
        }
    }

    public class RevokeDataAccess : CommandsTests
    {
        public RevokeDataAccess()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .RevokeReaderAccessToBotFilesAsync(A<string>.Ignored, cancellationToken))
                .Returns(Result.Ok());
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldCreateSuccessEmbed()
        {
            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.RevokeDataAccess(fixture.Create<string>());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GiveFailedExecutionShouldCreateErrorEmbed()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .RevokeReaderAccessToBotFilesAsync(A<string>.Ignored, cancellationToken))
                .Returns(Result.Fail("error message."));

            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.RevokeDataAccess(fixture.Create<string>());

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var email = fixture.Create<string>();
            var result = await commands.RevokeDataAccess(email);

            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveService>()
                .RevokeReaderAccessToBotFilesAsync(email, cancellationToken))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToCreateInteractionShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.RevokeDataAccess(fixture.Create<string>());

            result.IsSuccess.Should().BeFalse();
        }
    }

    public class DataUsage : CommandsTests
    {
        public DataUsage()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveSettingsService>()
                .GetConsumptionDataAsync(cancellationToken))
                .Returns(Result.Ok(fixture.Create<ConsumptionData>()));

            A.CallTo(() => fixture
                .FreezeFake<ChartService>()
                .CreatePieChart(A<ConsumptionData>.Ignored))
                .Returns(fixture.FreezeFake<Stream>());
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldCreateSuccessEmbed()
        {
            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);

            await commands.DataUsage();

            await Verify(embedResult);
        }

        [Fact]
        public async Task GiveFailedExecutionShouldCreateErrorEmbed()
        {
            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveSettingsService>()
                .GetConsumptionDataAsync(cancellationToken))
                .Returns(Result.Fail("error message."));

            Embed embedResult = null!;
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Invokes((Embed embed, FeedbackMessageOptions _, CancellationToken _) => embedResult = embed);
            
            await commands.DataUsage();

            await Verify(embedResult);
        }

        [Fact]
        public async Task GivenSuccessExecutionShouldReturnSuccess()
        {
            var result = await commands.DataUsage();
            result.IsSuccess.Should().BeTrue();

            A.CallTo(() => fixture
                .FreezeFake<GoogleDriveSettingsService>()
                .GetConsumptionDataAsync(cancellationToken))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fixture
                .FreezeFake<ChartService>()
                .CreatePieChart(A<ConsumptionData>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task GivenErrorToCreateInteractionShouldReturnError()
        {
            A.CallTo(() => fixture
                .FreezeFake<IFeedbackService>()
                .SendContextualEmbedAsync(A<Embed>.Ignored, A<FeedbackMessageOptions>.Ignored, cancellationToken))
                .Returns(Remora.Results.Result<IMessage>.FromError(new ValidationError("prop", "reason")));

            var result = await commands.DataUsage();

            result.IsSuccess.Should().BeFalse();
        }
    }
}
