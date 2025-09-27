using Microsoft.AspNetCore.Identity;

namespace PortalInmobiliario.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        string[] roleNames = { "Broker", "Cliente" };
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Crear un usuario Broker por defecto
        var brokerUser = new IdentityUser
        {
            UserName = "broker@inmobiliaria.com",
            Email = "broker@inmobiliaria.com",
        };

        var user = await userManager.FindByEmailAsync(brokerUser.Email);

        if (user == null)
        {
            var createPowerUser = await userManager.CreateAsync(brokerUser, "Broker123!");
            if (createPowerUser.Succeeded)
            {
                await userManager.AddToRoleAsync(brokerUser, "Broker");
            }
        }
    }
}