using Remora.Commands.Conditions;
namespace BotDeScans.App.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RoleAuthorizeAttribute(params string[] roleNames) : ConditionAttribute
{
    public readonly string[] RoleNames = roleNames;
}
