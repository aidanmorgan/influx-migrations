using InfluxDB.Client.Api.Domain;

namespace InfluxMigrations.Operations.Auth;

public static class TokenCommon
{
    private static readonly IDictionary<string, Permission.ActionEnum> ActionEnums =
        new Dictionary<string, Permission.ActionEnum>()
        {
            { "read", Permission.ActionEnum.Read },
            { "write", Permission.ActionEnum.Write }
        };

    public static Permission.ActionEnum? MapPermission(string? str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }
        
        if (ActionEnums.ContainsKey(str.ToLowerInvariant()))
        {
            return ActionEnums[str.ToLowerInvariant()];
        }

        return null;
    }
}