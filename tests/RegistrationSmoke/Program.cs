using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

DatabaseConnectionChecks.Run();

const string password = "TestOnly!2026";
foreach (var external in new[] { false, true })
{
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connection));
    services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();
    services.AddScoped<RegistrationService>();
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in AppRoles.All)
        Require((await roles.CreateAsync(new IdentityRole(role))).Succeeded, "Seed role");
    var registration = scope.ServiceProvider.GetRequiredService<RegistrationService>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var failed = await registration.RegisterAsync(NewUser("invalid"), "weak");
    Require(!failed.Succeeded && !await db.Users.AnyAsync(), "Failed registration leaves table empty");

    var first = NewUser("first");
    var login = external ? new UserLoginInfo("Test", "first", "Test") : null;
    Require((await registration.RegisterAsync(first, external ? null : password, login)).Succeeded, "First registration succeeds");
    db.ChangeTracker.Clear();
    Require((await db.Users.SingleAsync()).Role == UserRole.Administrator, "First account persists level 8");
    Require(await users.IsInRoleAsync(first, AppRoles.Administrator), "First account has Identity administrator membership");
    if (external)
        Require((await users.GetLoginsAsync(first)).Count == 1, "External login is saved");

    var second = NewUser("second");
    second.Role = UserRole.SuperAdministrator;
    Require((await registration.RegisterAsync(second, password)).Succeeded, "Next registration succeeds");
    Require(second.Role == UserRole.User && !await users.IsInRoleAsync(second, AppRoles.Administrator), "Next account cannot request administrator rights");

    if (external)
    {
        Require(!(await registration.RegisterAsync(NewUser("duplicate-login"), externalLogin: login)).Succeeded,
            "Duplicate external login is rejected");
        Require(await db.Users.CountAsync() == 2, "Failed external login rolls back account creation");
    }

    await db.Users.Where(u => u.Id == first.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.User));
    var third = NewUser("third");
    Require((await registration.RegisterAsync(third, password)).Succeeded && third.Role == UserRole.User,
        "Existing users without an administrator do not trigger bootstrap");
    Console.WriteLine($"PASS: {(external ? "External" : "Password")} registration checks");
}

static ApplicationUser NewUser(string name) => new($"{name}@example.test") { Email = $"{name}@example.test" };
static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
