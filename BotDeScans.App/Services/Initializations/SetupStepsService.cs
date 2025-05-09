using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services.Initializations;

public class SetupStepsService(
    IConfiguration configuration,
    IEnumerable<IStep> steps)
{
    public const string STEPS_KEY = "Settings:Publish:Steps";

    public Result Setup()
    {
        Console.WriteLine("Validating Publish Steps...");

        var expectedStepsAsString = configuration.GetValues<string>(STEPS_KEY, value => value);
        if (expectedStepsAsString.Length == 0)
            return Result.Fail($"Não foi encontrado nenhum passo de publicação em '{STEPS_KEY}'.");

        var expectedStepsNames = new List<StepName>();
        foreach (var configurationStepAsString in expectedStepsAsString)
        {
            if (!Enum.TryParse(typeof(StepName), configurationStepAsString, out var configurationStep))
                return Result.Fail($"{STEPS_KEY}: Não foi possível converter o tipo '{configurationStepAsString}' em um passo de publicação válido.");

            expectedStepsNames.Add((StepName)configurationStep);
        }

        return steps.Where(step => expectedStepsNames.Contains(step.Name))
                    .All(step => step.Type != StepType.Upload)
                        ? Result.Fail($"Não foi encontrado nenhum passo de upload de lançamentos em '{STEPS_KEY}'.")
                        : Result.Ok();
    }
}
