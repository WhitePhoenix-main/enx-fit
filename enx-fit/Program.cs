using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using enx_fit.Data;
using enx_fit.Services;
using enx_fit.Areas.Identity.Data;
using enx_fit.Security;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration
        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args);
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
var databaseConnection = DatabaseConnectionSettings.Resolve(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(databaseConnection));

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options => options.LoginPath = "/TechnicalPages/Login");
builder.Services.AddUserRoleAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<BodyMeasurementService>();
builder.Services.AddScoped<AnalyticsDataService>();
builder.Services.AddScoped<UserDirectoryService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddSingleton<TrainingAnalyticsService>();
builder.Services.AddSingleton<BodyAnalyticsService>();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Identity cannot build a ClaimsPrincipal from a malformed user-claim row.
    // Older databases may contain rows with a NULL/empty ClaimType, so remove
    // those unusable records before the first authenticated request is handled.
    var malformedClaims = await db.UserClaims
        .Where(claim => string.IsNullOrWhiteSpace(claim.ClaimType))
        .ToListAsync();
    if (malformedClaims.Count > 0)
    {
        app.Logger.LogWarning(
            "Removing {Count} malformed Identity user claim(s) with an empty ClaimType.",
            malformedClaims.Count);
        db.UserClaims.RemoveRange(malformedClaims);
        await db.SaveChangesAsync();
    }

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in AppRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create role '{roleName}': {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var users = await userManager.Users.ToListAsync();
    foreach (var user in users)
    {
        if (!await userManager.IsInRoleAsync(user, AppRoles.User))
        {
            var result = await userManager.AddToRoleAsync(user, AppRoles.User);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign the default role: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
    }
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    headers.TryAdd("X-Content-Type-Options", "nosniff");
    headers.TryAdd("Referrer-Policy", "no-referrer");
    headers.TryAdd("X-Frame-Options", "DENY");
    headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    headers.TryAdd(
        "Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; " +
        "font-src 'self' data:; " +
        "media-src 'self'; " +
        "connect-src 'self'; " +
        "frame-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "navigate-to 'self'; " +
        "frame-ancestors 'none'");
    headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
    headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");

    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
