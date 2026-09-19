using Npgsql;

namespace enx_fit.Data;

public static class DatabaseConnectionSettings
{
    public static string Resolve(IConfiguration configuration)
    {
        var connection = new NpgsqlConnectionStringBuilder(
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required."));

        var passwordFile = configuration["Database:PasswordFile"];
        var password = configuration["Database:Password"];
        if (!string.IsNullOrWhiteSpace(passwordFile))
        {
            // Secret files may end with a newline; preserve other password characters.
            password = File.ReadAllText(passwordFile).TrimEnd('\r', '\n');
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("The database password secret file is empty.");
        }

        if (password is not null)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Database:Password must not be empty.");
            connection.Password = password;
        }

        return connection.ConnectionString;
    }
}
