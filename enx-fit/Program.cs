using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using enx_fit.Data;
using enx_fit.Services;
using enx_fit.Areas.Identity.Data;
using enx_fit.Security;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<IdentityContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<IdentityContext>();
builder.Services.AddAuthorization(options =>
    options.AddPolicy(
        AppPolicies.AdministratorOnly,
        policy => policy.RequireRole(AppRoles.Administrator)));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<BodyMeasurementService>();
builder.Services.AddScoped<AnalyticsDataService>();
builder.Services.AddScoped<UserDirectoryService>();
builder.Services.AddSingleton<TrainingAnalyticsService>();
builder.Services.AddSingleton<BodyAnalyticsService>();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Dashboard");
    options.Conventions.AuthorizeFolder("/Workouts");
    options.Conventions.AuthorizeFolder("/Body");
    options.Conventions.AuthorizeFolder("/Analytics");
    options.Conventions.AuthorizeFolder("/Exercises");
    options.Conventions.AuthorizePage("/Exercises/Create", AppPolicies.AdministratorOnly);
    options.Conventions.AuthorizePage("/Exercises/Edit", AppPolicies.AdministratorOnly);
    options.Conventions.AuthorizePage("/Exercises/Delete", AppPolicies.AdministratorOnly);
    options.Conventions.AuthorizeFolder("/Admin", AppPolicies.AdministratorOnly);
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var identityContext = services.GetRequiredService<IdentityContext>();
    await identityContext.Database.MigrateAsync();

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

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
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

    var administratorEmail = builder.Configuration["IdentitySeed:AdministratorEmail"];
    var administrators = await userManager.GetUsersInRoleAsync(AppRoles.Administrator);
    if (administrators.Count == 0 && !string.IsNullOrWhiteSpace(administratorEmail))
    {
        var administrator = await userManager.FindByEmailAsync(administratorEmail);
        if (administrator is not null && !await userManager.IsInRoleAsync(administrator, AppRoles.Administrator))
        {
            var result = await userManager.AddToRoleAsync(administrator, AppRoles.Administrator);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign the administrator role: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
    }

    var applicationContext = services.GetRequiredService<ApplicationDbContext>();
    await applicationContext.Database.MigrateAsync();
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
