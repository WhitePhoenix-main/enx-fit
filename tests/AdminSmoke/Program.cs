using System.Net;
using System.Text.RegularExpressions;
using enx_fit.Areas.Identity.Data;
using enx_fit.Data;
using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Isolated test host: real compiled Razor pages, Identity cookies and one in-memory database.
// It never reads the application's connection string or creates data in its database.
var repository = new DirectoryInfo(AppContext.BaseDirectory);
while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "enx-fit", "enx-fit.csproj"))) repository = repository.Parent;
var root = Path.Combine(repository?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."), "enx-fit");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = root, WebRootPath = Path.Combine(root, "wwwroot"), ApplicationName = typeof(ApplicationDbContext).Assembly.GetName().Name, EnvironmentName = "Development" });
builder.Configuration.Sources.Clear();
builder.Configuration.AddInMemoryCollection();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
var portIndex = Array.IndexOf(args, "--port");
var port = portIndex >= 0 ? int.Parse(args[portIndex + 1]) : 5187;
var baseUrl = $"http://localhost:{port}";
builder.WebHost.UseUrls(baseUrl);
await using var dataConnection = new SqliteConnection("Data Source=:memory:");
await dataConnection.OpenAsync();
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(dataConnection));
builder.Services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "test-keys")));
builder.Services.AddUserRoleAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<UserDirectoryService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<BodyMeasurementService>();
builder.Services.AddRazorPages()
    .AddApplicationPart(typeof(AdminSmoke.Pages.RoleChecks.TrainerModel).Assembly);
await using var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
string adminId, memberId;
const string password = "TestOnly!2026";
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in AppRoles.All) await roles.CreateAsync(new IdentityRole(role));
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var admin = new ApplicationUser("admin@example.test") { Email = "admin@example.test", EmailConfirmed = true, Role = UserRole.SuperAdministrator };
    var member = new ApplicationUser("member@example.test") { Email = "member@example.test", EmailConfirmed = true };
    await users.CreateAsync(admin, password); await users.AddToRolesAsync(admin, AppRoles.All);
    await users.CreateAsync(member, password); await users.AddToRoleAsync(member, AppRoles.User);
    adminId = admin.Id; memberId = member.Id;
    for (var i = 1; i <= 13; i++)
    {
        var user = new ApplicationUser($"athlete{i:00}@example.test") { Email = $"athlete{i:00}@example.test", EmailConfirmed = i % 3 == 0 };
        await users.CreateAsync(user); await users.AddToRoleAsync(user, AppRoles.User);
    }
    for (var i = 0; i < 24; i++)
    {
        var workout = new WorkoutSession { UserId = member.Id, Title = new[] { "Силовая · Верх тела", "Ноги и кор", "Спина и плечи" }[i % 3], Date = DateOnly.FromDateTime(DateTime.Today).AddDays(-i), CreatedAtUtc = DateTime.UtcNow.AddDays(-i) };
        for (var e = 1; e <= 1 + i % 4; e++) workout.WorkoutExercises.Add(new WorkoutExercise { ExerciseId = e, Order = e });
        db.WorkoutSessions.Add(workout);
    }
    db.BodyMeasurements.Add(new BodyMeasurement { UserId = member.Id, WeightKg = 80, HeightCm = 180 });
    await db.SaveChangesAsync();
}
await app.StartAsync();
using var adminClient = Client();
using var memberClient = Client();
using var anonymous = Client();
var checks = 0;
try
{
    Check((await anonymous.GetAsync("/Admin")).StatusCode == HttpStatusCode.Redirect, "Anonymous admin access requires login");
    await Login(adminClient, "admin@example.test");
    await Login(memberClient, "member@example.test");
    foreach (var path in new[] { "/Workouts", "/Workouts/Create", "/Workouts/Edit/1", "/Workouts/Delete/1", "/Workouts/Details/1", "/Body", "/Body/Create", "/Body/Edit/1", "/Body/Delete/1", "/Analytics", "/Exercises", "/Exercises/Details/1", "/Exercises/Create", "/Exercises/Edit/1", "/Exercises/Delete/1", "/Dashboard" })
        Check(Denied(await anonymous.GetAsync(path)), $"Anonymous access denied: {path}");
    foreach (var path in new[] { "/Exercises/Create", "/Exercises/Edit/1", "/Exercises/Delete/1" })
    {
        Check(Denied(await memberClient.GetAsync(path)), $"Member cannot open exercise mutation page: {path}");
        var form = await Post(memberClient, path, new() { ["Input.Name"] = "Unauthorized exercise", ["Id"] = "1" }, "/Identity/Account/Login");
        Check(Denied(form), $"Member cannot POST exercise mutation: {path}");
    }
    Check((await memberClient.GetAsync("/Admin/Users")).Headers.Location?.ToString().Contains("AccessDenied") == true, "Member cannot access admin pages");
    var overview = await Page(adminClient, "/Admin");
    Check(overview.Contains("admin-activity-chart") && !overview.Contains("bootstrap.min.css") && overview.Contains("<svg"), "Dashboard renders original CSS and icons");
    await Page(adminClient, "/Admin?Days=7"); await Page(adminClient, "/Admin?Days=90"); await Page(adminClient, "/Admin?Days=-1");
    var filtered = await Page(adminClient, "/Admin/Users?Search=member");
    Check(filtered.Contains("member@example.test") && !filtered.Contains("athlete01@example.test"), "User search filters results");
    await Page(adminClient, "/Admin/Users?PageNumber=2"); await Page(adminClient, "/Admin/Users?PageNumber=999999");
    await Page(adminClient, "/Admin/Users?Role=Administrator"); await Page(adminClient, "/Admin/Users?Search=missing");
    Check((await anonymous.PostAsync("/Admin/Users/CreateUser", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode != HttpStatusCode.OK, "Anonymous user creation denied");
    Check((await adminClient.PostAsync("/Admin/Users/CreateUser", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode == HttpStatusCode.BadRequest, "POST requires antiforgery token");
    var create = await Post(adminClient, "/Admin/Users/CreateUser", new() { ["Input.Email"] = "new@example.test", ["Input.Password"] = password, ["Input.ConfirmPassword"] = password });
    Check(create.StatusCode == HttpStatusCode.Redirect, "User created through Razor form");
    var editUrl = create.Headers.Location!.ToString();
    await Page(adminClient, editUrl);
    var duplicate = await Post(adminClient, "/Admin/Users/CreateUser", new() { ["Input.Email"] = "new@example.test", ["Input.Password"] = password, ["Input.ConfirmPassword"] = password });
    Check(duplicate.StatusCode == HttpStatusCode.OK && (await duplicate.Content.ReadAsStringAsync()).Contains("field-validation-error"), "Duplicate email rejected");
    var edit = await Post(adminClient, editUrl, new() { ["Input.UserName"] = "new.name", ["Input.Email"] = "updated@example.test", ["Input.PhoneNumber"] = "+79991234567", ["Input.Role"] = "8" });
    Check(edit.StatusCode == HttpStatusCode.Redirect, "Profile and administrator role saved");
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var saved = await users.FindByEmailAsync("updated@example.test");
        Check(saved?.UserName == "new.name" && saved.PhoneNumber == "+79991234567" && saved.Role == UserRole.Administrator && await users.IsInRoleAsync(saved, AppRoles.Administrator), "Profile changes persisted atomically");
    }
    using var renamedClient = Client(); await Login(renamedClient, "updated@example.test");
    var self = await Post(adminClient, $"/Admin/Users/Edit/{adminId}", new() { ["Input.UserName"] = "admin@example.test", ["Input.Email"] = "admin@example.test", ["Input.Role"] = "1" });
    Check(self.StatusCode == HttpStatusCode.OK && (await self.Content.ReadAsStringAsync()).Contains("field-validation-error"), "Self-demotion rejected");
    await Page(adminClient, "/RoleChecks/Super");
    await Page(renamedClient, "/Admin/Users");
    Check(Denied(await renamedClient.GetAsync("/RoleChecks/Super")), "Level 8 cannot access a level 9 page");
    Check(Denied(await renamedClient.GetAsync($"/Admin/Users/Edit/{adminId}")), "Level 8 cannot edit level 9 profile");
    var escalation = await Post(renamedClient, $"/Admin/Users/Edit/{memberId}", new() { ["Input.UserName"] = "member@example.test", ["Input.Email"] = "member@example.test", ["Input.Role"] = "9" });
    Check(escalation.StatusCode == HttpStatusCode.OK && (await escalation.Content.ReadAsStringAsync()).Contains("field-validation-error"), "Cannot assign a role above the editor's own level");
    foreach (var invalidRole in new[] { "-1", "2", "10", "999", "invalid" })
    {
        var invalid = await Post(adminClient, editUrl, new() { ["Input.UserName"] = "new.name", ["Input.Email"] = "updated@example.test", ["Input.Role"] = invalidRole });
        Check(invalid.StatusCode == HttpStatusCode.OK && (await invalid.Content.ReadAsStringAsync()).Contains("field-validation-error"), "Invalid role rejected: " + invalidRole);
    }
    Check((await adminClient.GetAsync("/Admin/Users/Edit/missing")).StatusCode == HttpStatusCode.NotFound, "Unknown profile returns 404");
    Check((await adminClient.GetAsync("/Workouts/Create?userId=missing")).StatusCode == HttpStatusCode.NotFound, "Unknown workout owner returns 404");
    await Page(adminClient, $"/Workouts/Create?userId={memberId}");
    var workoutResponse = await Post(adminClient, "/Workouts/Create", new() { ["Input.OwnerId"] = memberId, ["Input.Date"] = DateTime.Today.ToString("yyyy-MM-dd"), ["Input.Title"] = "Smoke workout", ["Input.Notes"] = "Created by admin" });
    Check(workoutResponse.StatusCode == HttpStatusCode.Redirect, "Workout created for selected member");
    var workoutUrl = workoutResponse.Headers.Location!.ToString();
    await Page(adminClient, workoutUrl);
    var addExercise = await Post(adminClient, workoutUrl + "?handler=AddExercise", new() { ["AddExercise.ExerciseId"] = "1" });
    Check(addExercise.StatusCode == HttpStatusCode.Redirect, "Exercise added to workout");
    int workoutId = int.Parse(workoutUrl.Split('/').Last());
    int workoutExerciseId;
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workout = await db.WorkoutSessions.Include(w => w.WorkoutExercises).SingleAsync(w => w.Id == workoutId);
        Check(workout.UserId == memberId, "Workout belongs to requested member");
        workoutExerciseId = workout.WorkoutExercises.Single().Id;
    }
    var addSet = await Post(adminClient, workoutUrl + "?handler=AddSet", new() { ["AddSet.WorkoutExerciseId"] = workoutExerciseId.ToString(), ["AddSet.SetNumber"] = "1", ["AddSet.Weight"] = "60", ["AddSet.Reps"] = "10", ["AddSet.IsWarmup"] = "false" });
    Check(addSet.StatusCode == HttpStatusCode.Redirect, "Set added through workout form");
    var updateWorkout = await Post(adminClient, $"/Workouts/Edit/{workoutId}", new() { ["Id"] = workoutId.ToString(), ["Input.OwnerId"] = memberId, ["Input.Date"] = DateTime.Today.ToString("yyyy-MM-dd"), ["Input.Title"] = "Updated workout" });
    Check(updateWorkout.StatusCode == HttpStatusCode.Redirect, "Workout edit saved");
    var otherOwnerWorkout = await Post(adminClient, "/Workouts/Create", new() { ["Input.OwnerId"] = adminId, ["Input.Date"] = DateTime.Today.ToString("yyyy-MM-dd"), ["Input.Title"] = "Private test" });
    var privateUrl = otherOwnerWorkout.Headers.Location!.ToString();
    Check((await memberClient.GetAsync(privateUrl)).StatusCode == HttpStatusCode.NotFound, "Member cannot read another owner's workout");
    var revoke = await Post(adminClient, editUrl, new() { ["Input.UserName"] = "new.name", ["Input.Email"] = "updated@example.test", ["Input.Role"] = "1" });
    Check(revoke.StatusCode == HttpStatusCode.Redirect, "Administrator role can be removed from another user");
    Check(Denied(await renamedClient.GetAsync("/Admin/Users")), "Existing cookie loses admin access immediately after role edit");
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Check(await db.SetEntries.AnyAsync(s => s.WorkoutExerciseId == workoutExerciseId && s.Weight == 60 && s.Reps == 10), "Set persisted with expected values");
        Check(await db.WorkoutSessions.AnyAsync(w => w.Id == workoutId && w.Title == "Updated workout"), "Workout edit persisted");
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Check(!await users.IsInRoleAsync((await users.FindByEmailAsync("updated@example.test"))!, AppRoles.Administrator), "Removed role persisted");
    }
    await Page(adminClient, "/Workouts"); await Page(adminClient, "/Exercises"); await Page(adminClient, "/Exercises/Create");
    await Page(adminClient, "/Exercises/Edit/1"); await Page(adminClient, "/Exercises/Delete/1"); await Page(adminClient, "/Body"); await Page(adminClient, "/Body/Create");
    await Page(adminClient, "/Body/Edit/1"); await Page(adminClient, "/Body/Delete/1");
    var memberWorkouts = await Page(memberClient, "/Workouts");
    Check(!memberWorkouts.Contains("admin-shell"), "Member retains original layout");
    Check(Denied(await memberClient.GetAsync("/RoleChecks/Trainer")), "Level 1 cannot access a level 3 page");
    Check(Denied(await memberClient.PostAsync("/RoleChecks/Trainer", new FormUrlEncodedContent(new Dictionary<string, string>()))), "MinimumRole also protects POST requests");
    foreach (var level in new[] { UserRole.Trainer, UserRole.Moderator, UserRole.Administrator, UserRole.SuperAdministrator })
    {
        await SetRole(memberId, level);
        await Page(memberClient, "/RoleChecks/Trainer");
        Check(level >= UserRole.Administrator
            ? (await memberClient.GetAsync("/Admin")).StatusCode == HttpStatusCode.OK
            : Denied(await memberClient.GetAsync("/Admin")), $"SQL role {level} applied to existing cookie with correct hierarchy");
        Check(level == UserRole.SuperAdministrator
            ? (await memberClient.GetAsync("/RoleChecks/Super")).StatusCode == HttpStatusCode.OK
            : Denied(await memberClient.GetAsync("/RoleChecks/Super")), $"Level 9 restriction for {level}");
    }
    // Leave an old Administrator role membership: it must not override Role in the user table.
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await users.AddToRoleAsync((await users.FindByIdAsync(memberId))!, AppRoles.Administrator);
    }
    await SetRole(memberId, UserRole.User);
    Check(Denied(await memberClient.GetAsync("/Admin")), "SQL demotion overrides an existing cookie and legacy membership");
    using var legacyClient = Client();
    await Login(legacyClient, "member@example.test");
    Check(Denied(await legacyClient.GetAsync("/Admin")), "Fresh cookie with legacy Administrator claim cannot override numeric role");
    await SetRole(memberId, UserRole.Guest);
    await Page(memberClient, "/RoleChecks/Guest");
    Check(Denied(await memberClient.GetAsync("/Workouts")), "Guest level 0 cannot access member pages");
    Check(Denied(await memberClient.GetAsync("/Dashboard")), "Guest level 0 cannot access dashboard");
    Check(Denied(await anonymous.GetAsync("/RoleChecks/Guest")), "Guest enum still requires an authenticated account");
    await SetRole(memberId, UserRole.User);
    using var registeredClient = Client();
    var registration = await Post(registeredClient, "/Identity/Account/Register", new() { ["Input.Email"] = "registration@example.test", ["Input.Password"] = password, ["Input.ConfirmPassword"] = password, ["Input.Role"] = "9", ["Role"] = "9" });
    Check(registration.StatusCode == HttpStatusCode.Redirect, "Public registration works with ApplicationUser");
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Check((await db.Users.SingleAsync(u => u.Email == "registration@example.test")).Role == UserRole.User, "Public registration cannot assign a privileged role");
        var guest = new ApplicationUser("guest@example.test") { Email = "guest@example.test", Role = UserRole.Guest };
        db.Users.Add(guest);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Check((await db.Users.SingleAsync(u => u.Id == guest.Id)).Role == UserRole.Guest, "Explicit level 0 is preserved on insert despite database default 1");
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"AspNetUsers\" SET \"Role\" = {10} WHERE \"Id\" = {guest.Id}");
            Check(false, "Database rejects out-of-range role values");
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            Check(true, "Database rejects out-of-range role values");
        }
    }
    Console.WriteLine($"PASS: {checks} integration checks.");
    if (args.Contains("--serve")) { Console.WriteLine($"Test preview: {baseUrl}/Admin (admin@example.test / TestOnly!2026). In-memory test data only."); await app.WaitForShutdownAsync(); }
}
finally { await app.StopAsync(); }

HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(baseUrl) };
bool Denied(HttpResponseMessage response) => response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Redirect && (response.Headers.Location?.ToString().Contains("AccessDenied") == true || response.Headers.Location?.ToString().Contains("Login") == true);
async Task SetRole(string id, UserRole role)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"AspNetUsers\" SET \"Role\" = {(int)role} WHERE \"Id\" = {id}");
}
void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
async Task<string> Page(HttpClient client, string url)
{
    var response = await client.GetAsync(url); var html = await response.Content.ReadAsStringAsync();
    if (response.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"GET {url}: {response.StatusCode}\n{Regex.Replace(html, "<[^>]+>", " ")[..Math.Min(2500, Regex.Replace(html, "<[^>]+>", " ").Length)]}");
    checks++; return html;
}
async Task<HttpResponseMessage> Post(HttpClient client, string url, Dictionary<string, string> fields, string? tokenPage = null)
{
    var html = await Page(client, tokenPage ?? url.Split('?')[0]);
    fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    return await client.PostAsync(url, new FormUrlEncodedContent(fields));
}
async Task Login(HttpClient client, string email)
{
    var response = await Post(client, "/Identity/Account/Login", new() { ["Input.Email"] = email, ["Input.Password"] = password });
    Check(response.StatusCode == HttpStatusCode.Redirect, "Identity login: " + email);
}
