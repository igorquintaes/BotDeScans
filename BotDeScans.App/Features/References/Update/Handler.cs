using BotDeScans.App.Extensions;
using BotDeScans.App.Infra.Repositories;
using FluentResults;
using FluentValidation;

namespace BotDeScans.App.Features.References.Update;

public class Handler(
    IValidator<Request> requestValidator,
    TitleRepository titleRepository)
{
    public virtual async Task<Result> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var validationResult = requestValidator.Validate(request);
        if (validationResult.IsValid is false)
            return validationResult.ToResult();

        var title = await titleRepository.GetTitleAsync(request.TitleId, cancellationToken);
        if (title is null)
            return Result.Fail("Obra não encontrada.");

        title.AddOrUpdateReference(request.ReferenceKey, request.ReferenceValue);

        await titleRepository.SaveAsync(cancellationToken);

        return Result.Ok();
    }
}
