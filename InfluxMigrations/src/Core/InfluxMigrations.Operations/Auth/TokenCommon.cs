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

    private static readonly IDictionary<string, AuthorizationUpdateRequest.StatusEnum> StatusEnums =
        new Dictionary<string, AuthorizationUpdateRequest.StatusEnum>()
        {
            { "active", AuthorizationUpdateRequest.StatusEnum.Active },
            { "inactive", AuthorizationUpdateRequest.StatusEnum.Inactive }
        };

    public static AuthorizationUpdateRequest.StatusEnum? MapStatus(string? str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        if (StatusEnums.ContainsKey(str.ToLowerInvariant()))
        {
            return StatusEnums[str.ToLowerInvariant()];
        }

        return null;
    }

    public static AuthorizationUpdateRequest.StatusEnum? InvertStatus(AuthorizationUpdateRequest.StatusEnum? status)
    {
        if (status == null)
        {
            return null;
        }

        switch (status)
        {
            case AuthorizationUpdateRequest.StatusEnum.Active:
            {
                return AuthorizationUpdateRequest.StatusEnum.Inactive;
            }

            case AuthorizationUpdateRequest.StatusEnum.Inactive:
            {
                return AuthorizationUpdateRequest.StatusEnum.Active;
            }

            default:
            {
                return null;
            }
        }
    }
}