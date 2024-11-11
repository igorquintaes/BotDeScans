using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Features.Publish.Steps;

public class PublishTsukiStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadTsuki;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => TryGetMangaId(state.Info.DisplayTitle, out var _) is false
        ? Task.FromResult(Result.Fail("Obra não encontrada em 'tsuki-ids.txt'."))
        : Task.FromResult(Result.Ok());

    // todo: validação de 100MB
    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var tsukiService = serviceProvider.GetRequiredService<TsukiService>();

        // We can't call TsukiClient directly because Microsoft's AddHttpClient registers it as Transient.
        // So retreiving it from ServiceProvider here is going to have a different reference, not initialized.
        var loginResult = await tsukiService.LoginAsync(cancellationToken);
        if (loginResult.IsFailed)
            return loginResult;

        var uploadResult = await tsukiService.UploadChapterAsync(
            state.Info.DisplayTitle,
            state.InternalData.ZipFilePath,
            cancellationToken);

        if (uploadResult.IsFailed)
            return uploadResult.ToResult();

        state.Links.TsukiLink = uploadResult.Value;
        return Result.Ok();
    }

    // isso vai morrer, pode continuar feio
    private static bool TryGetMangaId(string mangaName, out string mangaId) =>
        File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "tsuki-ids.txt"))
            .Select(x => x.Split("$"))
            .ToDictionary(x => x[0].Trim().ToLowerInvariant(), x => x[1].Trim())
            .TryGetValue(mangaName.ToLowerInvariant(), out mangaId!);
}
