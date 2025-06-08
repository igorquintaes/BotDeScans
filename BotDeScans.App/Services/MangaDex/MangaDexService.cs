using BotDeScans.App.Extensions;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services.MangaDex.InternalServices;
using FluentResults;
using MangaDexSharp;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services.MangaDex;

public partial class MangaDexService(
    MangaDexUploadService mangaDexUploadService,
    IConfiguration configuration)
{
    public virtual async Task<Result<Chapter>> UploadAsync(
        Info info,
        string titleId,
        string filesDirectory,
        CancellationToken cancellationToken)
    {
        var existingSessionResult = await mangaDexUploadService.GetOpenSessionAsync();
        if (existingSessionResult.IsFailed)
            return existingSessionResult.ToResult();

        var existingSession = existingSessionResult.ValueOrDefault;
        if (string.IsNullOrWhiteSpace(existingSession?.Id) is false)
        {
            var abandonSessionResult = await mangaDexUploadService.AbandonSessionAsync(existingSession.Id);
            if (abandonSessionResult.IsFailed)
                return abandonSessionResult;
        }

        var groupId = configuration.GetRequiredValue<string>("Mangadex:GroupId");

        return await mangaDexUploadService.UploadFilesAsync(
            filesDirectory,
            titleId,
            groupId,
            info,
            cancellationToken);
    }
}
