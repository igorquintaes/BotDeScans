using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Services.Initializations.Factories;
using FluentResults;
using MangaDexSharp;
using MangaDexSharp.Utilities.Upload;

namespace BotDeScans.App.Services.MangaDex.InternalServices;

public class MangaDexUploadService(
    MangaDexAccessToken mangaDexAccessToken,
    IUploadUtilityService uploadUtilityService,
    IMangaDex mangaDex)
{
    public virtual async Task<Result<UploadSession>> GetOpenSessionAsync()
    {
        const int NOT_FOUND = 404;
        var sessionResponse = await mangaDex.Upload.Get(default, mangaDexAccessToken.Value);
        return sessionResponse.AsResult(allowedStatusCodes: NOT_FOUND);
    }

    public virtual async Task<Result> AbandonSessionAsync(
        string sessionId)
    {
        var abandonResponse = await mangaDex.Upload.Abandon(sessionId, mangaDexAccessToken.Value);
        return abandonResponse.AsResult();
    }

    public virtual async Task<Chapter> UploadFilesAsync(
        string directory,
        string titleId,
        string groupId,
        Info info,
        CancellationToken cancellationToken)
    {
        var session = await uploadUtilityService.New(titleId, [groupId], c => c
            .WithAuthToken(mangaDexAccessToken.Value)
            .WithCancellationToken(cancellationToken)
            .WithMaxBatchSize(10)
            .WithPageOrderFactory(file => file.OrderBy(x => x.Attributes!.OriginalFileName)));

        foreach (var file in Directory.GetFiles(directory))
            await session.UploadFile(file);

        return await session.Commit(new()
        {
            Chapter = info.ChapterNumber,
            Volume = info.ChapterVolume,
            Title = info.ChapterName,
            TranslatedLanguage = info.Language // todo: suport more languages after app internationalization
        });
    }
}
