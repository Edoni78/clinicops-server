using Microsoft.AspNetCore.Identity;

namespace ClinicOps.Infrastructure.Data.Seed
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var roles = new[] { "ClinicAdmin", "Doctor", "Nurse", "LabTechnician", "Manager" };

            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));
            }
        }
    }
}