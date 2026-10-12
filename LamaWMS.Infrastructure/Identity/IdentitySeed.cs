using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LamaWMS.Infrastructure.Identity;

public static class IdentitySeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "Operator", "Viewer" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ",
                            result.Errors.Select(x => x.Description)));
            }
        }

        const string adminLogin = "admin";

        var admin = await userManager.FindByNameAsync(adminLogin);

        if (admin == null)
        {
            const string password = "Admin123!";

            admin = new ApplicationUser
            {
                UserName = adminLogin,
                FullName = "Super Administrator"
            };

            var result = await userManager.CreateAsync(admin, password);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ",
                        result.Errors.Select(x => x.Description)));

            result = await userManager.AddToRoleAsync(admin, "Admin");

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ",
                        result.Errors.Select(x => x.Description)));
        }
    }
}