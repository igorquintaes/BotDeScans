using BotDeScans.App.Extensions;
using FluentResults;
using FluentValidation;

namespace BotDeScans.App.Features.References.Update;

public class Handler(
    IValidator<Request> requestValidator,
    Persistence persistence)
{
    public async Task<Result> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var validationResult = requestValidator.Validate(request);
        if (validationResult.IsValid is false)
            return validationResult.ToResult();

        var title = await persistence.GetTitleAsync(request.TitleId, cancellationToken);

        title.AddOrUpdateReference(request.ReferenceKey, request.ReferenceValue);

        await persistence.SaveAsync(cancellationToken);

        return Result.Ok();
    }
}
