using Remora.Commands.Conditions;

namespace BotDeScans.App.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RoleAuthorizeAttribute(string roleName) : ConditionAttribute
{
    public readonly string RoleName = roleName;
}
