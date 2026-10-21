using enx_fit.Data;
using enx_fit.Extensions;
using enx_fit.Models;
using enx_fit.Pages.Infrastructure;
using enx_fit.Presentation.Exercises;
using enx_fit.Security;
using enx_fit.Services.Accounts;
using enx_fit.Services.Exercises;
using enx_fit.Services.Workouts;
using enx_fit.ViewModels.Exercises;
using enx_fit.ViewModels.Workouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace enx_fit.Pages.Training.Workouts;

[MinimumRole(UserRole.User)]
public class CreateModel(
    WorkoutService workoutService,
    CurrentUser currentUser,
    UserDirectoryService userDirectory,
    ApplicationDbContext db,
    ExerciseService exerciseService) : OwnerFormPageModel(currentUser, userDirectory)
{
    [BindProperty]
    public WorkoutSessionInputModel Input { get; set; } = new();

    [BindProperty, System.ComponentModel.DataAnnotations.StringLength(200000)]
    public string? BuilderJson { get; set; }
    public string ViewerId => Viewer.Id;
    public object BuilderData { get; private set; } = new { };
    public ExerciseLibraryModel Library { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        Input.Date = Viewer.LocalToday;
        Input.OwnerId = Viewer.Id;
        if (IsAdministrator && !string.IsNullOrWhiteSpace(userId))
        {
            if (!await UserDirectory.ExistsAsync(userId)) return NotFound();
            Input.OwnerId = userId;
        }
        await LoadPageAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        WorkoutBuilderInput? builder = null;
        var validOwner = await OwnerIsValidAsync(Input.OwnerId);
        if (BuilderJson is not null)
        {
            if (BuilderJson.Length > 200000 || !WorkoutBuilderInput.TryParse(BuilderJson, out builder))
                ModelState.AddModelError("", "Проверьте структуру тренировки: до 40 упражнений, вес 0–2000 кг, 1–1000 повторений и отдых до 900 секунд.");
            else
            {
                var ids = builder!.Blocks.SelectMany(b => b.Exercises).Select(e => e.ExerciseId).ToArray();
                if (ids.Length == 0) ModelState.AddModelError("", "Добавьте хотя бы одно упражнение из библиотеки.");
                else if (await db.Exercises.CountAsync(e => ids.Contains(e.Id)) != ids.Length)
                    ModelState.AddModelError("", "Одно из упражнений больше не доступно. Выберите упражнение из библиотеки.");
            }
        }
        if (!validOwner || !ModelState.IsValid)
        {
            await LoadPageAsync();
            return Page();
        }

        var id = await workoutService.CreateAsync(Input, builder);
        TempData["StatusMessage"] = "Тренировка создана.";
        return RedirectToPage("./Details", new { id });
    }

    private async Task LoadPageAsync()
    {
        await LoadUsersAsync();
        var exercises = await exerciseService.GetAllAsync(null);
        var owner = Viewer.ResolveOwnerId(Input.OwnerId);
        var history = await db.WorkoutSessions.AsNoTracking().AsSplitQuery()
            .Where(w => w.UserId == owner).Where(WorkoutStates.Completed)
            .Include(w => w.WorkoutExercises).ThenInclude(e => e.SetEntries)
            .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).Take(50).ToListAsync();
        var last = history.FirstOrDefault();
        var weight = await db.BodyMeasurements.AsNoTracking().Where(b => b.UserId == owner)
            .OrderByDescending(b => b.Date).Select(b => (decimal?)b.WeightKg).FirstOrDefaultAsync();
        Library = new ExerciseLibraryModel
        {
            Id = "wb-library", ViewerId = Viewer.Id, Exercises = exercises, ForBuilder = true,
            RecentIds = await exerciseService.GetRecentIdsAsync(owner)
        };
        BuilderData = new
        {
            exercises = exercises.Select(e => new { e.Id, Name = ExercisePresentation.DisplayName(e), OriginalName = e.Name, e.MuscleGroup, e.Equipment }),
            records = history.SelectMany(w => w.WorkoutExercises).GroupBy(e => e.ExerciseId).Select(g => new
            {
                exerciseId = g.Key,
                weight = g.SelectMany(e => e.SetEntries).Where(s => s.IsCompleted && !s.IsWarmup).Select(s => s.Weight).DefaultIfEmpty().Max()
            }),
            previous = last is null ? null : new
            {
                title = last.Title, exercises = last.WorkoutExercises.Count,
                sets = last.WorkoutExercises.Sum(e => e.SetEntries.Count(s => s.IsCompleted && !s.IsWarmup)),
                volume = last.WorkoutExercises.Sum(e => e.SetEntries.Where(s => s.IsCompleted && !s.IsWarmup).Sum(s => s.Volume))
            },
            bodyWeight = weight, ownerId = owner,
            hasErrors = !ModelState.IsValid
        };
    }
}
