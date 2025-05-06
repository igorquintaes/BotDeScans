using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Factories;
using FluentResults;
using MangaDexSharp;

namespace BotDeScans.App.Services.MangaDex.InternalServices;

public class MangaDexUploadService(
    MangaDexAccessToken mangaDexAccessToken, 
    FileService fileService,
    IMangaDex mangaDex)
{
    public virtual async Task<Result<UploadSession>> GetOpenSessionAsync()
    {
        const int NOT_FOUND = 404;
        var sessionResponse = await mangaDex.Upload.Get(mangaDexAccessToken.Value);
        return sessionResponse.AsResult(NOT_FOUND);
    }

    public virtual async Task<Result> AbandonSessionAsync(
        string sessionId)
    {
        var abandonResponse = await mangaDex.Upload.Abandon(sessionId, mangaDexAccessToken.Value);
        return abandonResponse.AsResult();
    }

    public virtual async Task<Result<UploadSession>> CreateSessionAsync(
        string titleId,
        string groupId)
    {
        var createSessionResponse = await mangaDex.Upload.Begin(titleId, [groupId], mangaDexAccessToken.Value);
        return createSessionResponse.AsResult();
    }

    public virtual async Task<Result<string[]>> UploadFilesAsync(
        string directory,
        string sessionId,
        CancellationToken cancellationToken)
    {
        const int MAX_CHUNK_FILES = 10;
        const long MAX_CHUNK_BYTES = 150 * 1024 * 1024; // 150MB

        var pageIds = new List<string>();
        var filesPaths = Directory
            .GetFiles(directory)
            .OrderBy(x => x);

        var chunks = fileService.CreateChunks(filesPaths, MAX_CHUNK_FILES, MAX_CHUNK_BYTES);
        foreach (var chunk in chunks)
        {
            using (chunk)
            {
                var files = chunk.Files
                    .Select(data => new StreamFileUpload(data.Key, data.Value))
                    .ToArray();

                var uploadResponse = await mangaDex.Upload.Upload(
                    sessionId,
                    mangaDexAccessToken.Value,
                    cancellationToken,
                    files);

                var uploadResult = uploadResponse.AsResult();
                if (uploadResult.IsFailed)
                    return uploadResult.ToResult();

                pageIds.AddRange(uploadResult.Value.Select(x => x.Id).ToArray());
            }
        }

        return Result.Ok(pageIds.ToArray());
    }

    public virtual async Task<Result<Chapter>> CommitSessionAsync(
        string sessionId,
        string? chapterName,
        string chapterNumber,
        string? volume,
        string[] pagesIds)
    {
        var sessionData = new UploadSessionCommit
        {
            Chapter = new()
            {
                Chapter = chapterNumber,
                Volume = volume,
                Title = chapterName,
                TranslatedLanguage = "pt-br" // todo: parametrizar após internacionalização
            },
            PageOrder = pagesIds
        };

        var uploadCommitResult = await mangaDex.Upload.Commit(sessionId, sessionData, mangaDexAccessToken.Value);
        return uploadCommitResult.AsResult();
    }
}
