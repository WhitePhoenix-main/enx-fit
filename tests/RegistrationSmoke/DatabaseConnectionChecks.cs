using enx_fit.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

internal static class DatabaseConnectionChecks
{
    public static void Run()
    {
        var neonConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=ep-damp-violet-b1prt7h4.c-5.eu-central-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;SSL Mode=VerifyFull;Channel Binding=Require"
        }).Build();
        var neonConnection = new NpgsqlConnectionStringBuilder(DatabaseConnectionSettings.Resolve(neonConfiguration));
        if (neonConnection.SslMode != SslMode.VerifyFull || neonConnection.ChannelBinding != ChannelBinding.Require)
            throw new InvalidOperationException("Neon connection settings were not preserved.");
        Console.WriteLine("PASS: Neon connection string syntax (no database connection attempted)");

        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=legacy"
        };
        string ResolvePassword() => new NpgsqlConnectionStringBuilder(DatabaseConnectionSettings.Resolve(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build())).Password!;
        void Require(bool condition)
        {
            if (!condition) throw new InvalidOperationException("Database secret resolution check failed.");
        }

        Require(ResolvePassword() == "legacy");
        values["Database:Password"] = "test;password=with\"special characters ";
        Require(ResolvePassword() == values["Database:Password"]);

        var path = Path.GetTempFileName();
        try
        {
            values["Database:PasswordFile"] = path;
            File.WriteAllText(path, "file;password \r\n");
            Require(ResolvePassword() == "file;password ");
            File.WriteAllText(path, "\r\n");
            var rejected = false;
            try { ResolvePassword(); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected);

            values["Database:PasswordFile"] = path + ".missing";
            rejected = false;
            try { ResolvePassword(); }
            catch (FileNotFoundException) { rejected = true; }
            Require(rejected);
        }
        finally { File.Delete(path); }
        Console.WriteLine("PASS: Database secrets, precedence, special characters and missing/empty file checks");
    }
}
