using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace enx_fit.Data;

// Migration generation must not execute application startup or connect to the user's database.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var directory = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(directory, "appsettings.json"))) directory = Path.Combine(directory, "enx-fit");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configurationBuilder = new ConfigurationBuilder().SetBasePath(directory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);
        if (environment == "Development") configurationBuilder.AddJsonFile("appsettings.Local.json", optional: true)
            .AddUserSecrets(typeof(ApplicationDbContext).Assembly, optional: true);
        var configuration = configurationBuilder.AddEnvironmentVariables().Build();
        // Constructing options reads configuration only. EF opens the connection for database commands.
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionSettings.Resolve(configuration)).Options);
    }
}
