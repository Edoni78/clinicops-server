using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClinicOps.Infrastructure.Data
{
    /// <summary>
    /// Used by EF Core tools (e.g. dotnet ef) at design time to create ApplicationDbContext.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            );

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
