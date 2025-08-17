using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadSakuraMangasStep(
    SakuraMangasService sakuraMangasService,
    State state) : IPublishStep
{
    // todo: será necessário criar um campo com flag sobre continuar a execução em casos de erro;
    // por exemplo, não temos como validar ainda com a SakuraMangás erros como 
    // o upload de páginas ainda não criadas para os projetos (MANGA_NOT_FOUND)
    // ou já existente por outra scan (CHAPTER_EXISTS_OTHER_SCAN) e demais cenários.
    // Como não é possível validar antes do upload, devemos permitir falhar e continuar a execução.

    public StepName? Dependency => StepName.ZipFiles;

    public StepName Name => StepName.UploadSakuraMangas;

    public StepType Type => StepType.Upload;

    public Task<Result> ValidateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Ok()); // Todo: validação de arquivo com max de 100mb (pós zip)

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var uploadResult = await sakuraMangasService.UploadAsync(
            state.ChapterInfo.ChapterNumber,
            state.ChapterInfo.ChapterName,
            state.Title.References.Single(x => x.Key == ExternalReference.SakuraMangas).Value,
            state.InternalData.ZipFilePath!,
            cancellationToken);

        if (uploadResult.IsFailed)
            return uploadResult.ToResult();

        state.ReleaseLinks.SakuraMangas = uploadResult.Value;
        return Result.Ok();
    }
}
