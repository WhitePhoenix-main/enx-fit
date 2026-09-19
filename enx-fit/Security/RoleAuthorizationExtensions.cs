using Microsoft.AspNetCore.Authentication;

namespace enx_fit.Security;

public static class RoleAuthorizationExtensions
{
    public static IServiceCollection AddUserRoleAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IClaimsTransformation, UserRoleClaimsTransformation>();
        services.AddAuthorization(options =>
        {
            foreach (var role in Enum.GetValues<UserRole>())
            {
                options.AddPolicy(MinimumRoleAttribute.PolicyName(role), policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context => context.User.HasMinimumRole(role)));
            }

            options.AddPolicy(AppPolicies.AdministratorOnly, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.HasMinimumRole(UserRole.Administrator)));
        });
        return services;
    }

}
