namespace enx_fit.Security;

public static class AppRoles
{
    public const string User = "User";
    public const string Administrator = "Administrator";

    public static readonly string[] All = [User, Administrator];
}

public static class AppPolicies
{
    public const string AdministratorOnly = "AdministratorOnly";
}
