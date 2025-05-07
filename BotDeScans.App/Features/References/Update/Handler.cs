using BotDeScans.App.Extensions;
using BotDeScans.App.Infra;
using FluentResults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Features.References.Update;

public class Handler(
    IValidator<Request> requestValidator,
    DatabaseContext context)
{
    public async Task<Result> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var validationResult = requestValidator.Validate(request);
        if (validationResult.IsValid is false)
            return validationResult.ToResult();

        var title = await context.Titles
            .Include(x => x.References)
            .Where(x => x.Name == request.Title)
            .SingleAsync(cancellationToken);
        
        title.AddOrUpdateReference(request.ReferenceKey, request.ReferenceId);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
