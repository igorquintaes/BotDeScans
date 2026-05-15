namespace BotDeScans.App.Features.Publish.Interaction.Models;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ReleaseLinkAttribute(string label) : Attribute
{
    public string Label { get; } = label;
}
