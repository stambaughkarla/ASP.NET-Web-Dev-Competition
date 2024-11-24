using Microsoft.AspNetCore.Identity;
using FinalProject.Models;

namespace FinalProject.Seeding
{
    public class SeedRoles
    {
        public static async Task AddAllRoles(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("Customer"))
            {
                await roleManager.CreateAsync(new IdentityRole("Customer"));
            }

            if (!await roleManager.RoleExistsAsync("Host"))
            {
                await roleManager.CreateAsync(new IdentityRole("Host"));
            }
        }
    }
}
