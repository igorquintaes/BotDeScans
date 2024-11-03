using BotDeScans.App.Enums;
using BotDeScans.App.Extensions;
using BotDeScans.App.Models;
using FluentResults;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Serilog;
using System.Drawing;
namespace BotDeScans.App.Builders;

public static class EmbedBuilder
{
    public static Embed CreateErrorEmbed(ResultBase result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("error embed must be called with an error result state");

        var errorsInfos = result.Errors.GetErrorsInfo();
        var embedFields = errorsInfos.Select(errorInfo =>
            new EmbedField(
                Name: errorInfo switch
                {
                    _ when errorInfo.Type == ErrorType.Exception => ":warning: Detalhe de exceção",
                    _ when errorInfo.Depth != 0 => ":arrow_right: Detalhe interno",
                    _ => $"Erro {errorInfo.Number}"
                },
                Value: errorInfo.Message))
            .ToList();

        // todo: some errors can be larger than embed max lenght.
        // Log a duplicate info is a short and fast way to prevent info loss.
        // We need handle it better, creating N embeds or truncating messages.
        foreach (var embedField in embedFields)
            Log.Error($"{embedField.Name}{Environment.NewLine}{embedField.Value}");

        return new Embed(
            Title: ":no_entry: Erro!",
            Colour: Color.Red,
            Fields: embedFields);
    }

    public static Embed CreateSuccessEmbed(
        Optional<string> title = default,
        Optional<IEmbedImage> image = default)
        => new(Title: title.HasValue ? title : "Sucesso!",
               Colour: Color.Green,
               Image: image);

    // todo - move to other class. it is embed class
    public static async Task<Remora.Results.Result<IMessage>> HandleTasksAndUpdateMessage(
        IDictionary<StepEnum, StepStatus> steps,
        InteractionContext interactionContext,
        Remora.Results.Result<IMessage> reply,
        IDiscordRestInteractionAPI discordRestInteractionAPI,
        CancellationToken cancellationToken = default)
    {
        var tasks = new BotTasks(steps);
        var embed = new Embed(
            Title: tasks.Header,
            Description: tasks.Details,
            Colour: tasks.ColorStatus);

        return await discordRestInteractionAPI.EditFollowupMessageAsync(
            reply.Entity.Author.ID,
            interactionContext.Interaction.Token,
            messageID: reply.Entity.ID,
            embeds: new List<Embed> { embed },
            ct: cancellationToken);
    }
}
