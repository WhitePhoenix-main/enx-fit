using System.Globalization;
using enx_fit.Extensions;
using enx_fit.Models;
using enx_fit.Pages;
using enx_fit.Services;

namespace enx_fit.ViewModels;

public sealed record WorkoutExercisePreview(int Id, string Name, string Summary, string Photo);
public sealed record WorkoutHistoryItem(int Id, DateOnly Date, string Title, int Exercises, int Sets,
    decimal Volume, string Photo, int? ProgramId, string ProgramName, IReadOnlyList<WorkoutExercisePreview> Preview);
public sealed record WorkoutCalendarDay(DateOnly Date, string Status, int? WorkoutId, string Title,
    int? PlannedWorkoutId = null, int? ProgramId = null);

public sealed class DashboardWorkoutsViewModel
{
    public IReadOnlyList<WorkoutHistoryItem> History { get; }
    public WorkoutHistoryItem? Selected { get; }
    public IReadOnlyList<WorkoutCalendarDay> Calendar { get; }
    public IReadOnlyList<(string Value, string Name)> Programs { get; }
    public int MonthlyWorkouts { get; }
    public int MonthlySets { get; }
    public int WeeklyWorkouts { get; }
    public int WeeklyTarget { get; }
    public DateOnly Month { get; }
    public static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    public static string Number(decimal value) => value.ToString("#,0.##", Russian).Replace('\u00a0', ' ');
    private static string Plural(int count, string one, string few, string many) =>
        count % 100 is >= 11 and <= 14 ? many : count % 10 == 1 ? one : count % 10 is >= 2 and <= 4 ? few : many;
    public static string ExerciseCount(int count) => $"{count} {Plural(count, "упражнение", "упражнения", "упражнений")}";
    public static string SetCount(int count) => $"{count} {Plural(count, "рабочий подход", "рабочих подхода", "рабочих подходов")}";

    public DashboardWorkoutsViewModel(DashboardModel model)
    {
        var data = model.Data;
        var today = model.Reference ? new DateOnly(2026, 10, 2) : data.Today;
        Month = model.Month ?? new DateOnly(today.Year, today.Month, 1);
        var all = model.Reference ? ReferenceHistory() : data.Completed.Select(w => Present(w, model.WorkoutPrograms)).ToList();
        var monthly = all.Where(w => w.Date >= today.AddDays(-29) && w.Date <= today).ToList();
        MonthlyWorkouts = model.Reference ? 8 : monthly.Count;
        MonthlySets = model.Reference ? 96 : monthly.Sum(w => w.Sets);
        WeeklyWorkouts = model.Reference ? 2 : data.ThisWeek.Count();
        WeeklyTarget = model.Reference ? 3 : data.Settings.WeeklyWorkoutGoal;
        Programs = all.Select(w => (Value: w.ProgramId?.ToString() ?? "standalone", Name: w.ProgramName))
            .Distinct().OrderBy(p => p.Name).ToList();
        var until = model.Reference ? today : data.Until;
        History = all.Where(w => model.WorkoutDate is { } date ? w.Date == date : w.Date >= until.AddDays(1 - model.Days) && w.Date <= until)
            .Where(w => string.IsNullOrWhiteSpace(model.Search) || w.Title.Contains(model.Search, StringComparison.OrdinalIgnoreCase) ||
                w.Preview.Any(e => e.Name.Contains(model.Search, StringComparison.OrdinalIgnoreCase)))
            .Where(w => string.IsNullOrEmpty(model.WorkoutProgram) || (w.ProgramId?.ToString() ?? "standalone") == model.WorkoutProgram)
            .OrderByDescending(w => w.Date).ThenByDescending(w => w.Id).ToList();
        Selected = History.FirstOrDefault(w => w.Id == model.SelectedWorkout) ?? History.FirstOrDefault();
        var start = Month.AddDays(-((int)Month.DayOfWeek + 6) % 7);
        var cells = (int)Math.Ceiling((((int)Month.DayOfWeek + 6) % 7 + DateTime.DaysInMonth(Month.Year, Month.Month)) / 7d) * 7;
        var planned = data.Workouts.Where(w => !w.StartedAtUtc.HasValue && !w.CompletedAtUtc.HasValue && !DashboardData.WorkingSets(w).Any()).ToList();
        var occurrences = model.WorkoutPrograms.Where(p => !p.IsArchived).SelectMany(ProgramSchedule.Occurrences).ToList();
        Calendar = Enumerable.Range(0, cells).Select(i =>
        {
            var date = start.AddDays(i);
            var done = all.FirstOrDefault(w => w.Date == date);
            var plan = planned.FirstOrDefault(w => w.Date == date);
            var program = occurrences.FirstOrDefault(o => o.Date == date);
            var status = done is not null ? "completed" : date >= today && (plan is not null || program is not null) ? "planned" : "none";
            if (model.Reference)
                status = date.Year == 2026 && date.Month == 10
                    ? new[] { 1, 2, 3, 6, 8, 13, 20, 22, 29 }.Contains(date.Day) ? "completed"
                    : new[] { 10, 17, 24, 31 }.Contains(date.Day) ? "planned" : "none" : "none";
            return new WorkoutCalendarDay(date, status, done?.Id, done?.Title ?? plan?.Title ?? program?.Workout.Name ?? "Нет тренировки",
                plan?.Id, program?.Workout.TrainingProgramId);
        }).ToList();
    }

    private static WorkoutHistoryItem Present(WorkoutSession workout, IReadOnlyList<TrainingProgram> programs)
    {
        var exercises = workout.WorkoutExercises.OrderBy(e => e.Order).Select(e =>
        {
            var sets = e.SetEntries.Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0).ToList();
            var name = ExercisePresentation.DisplayName(e.Exercise);
            string summary;
            if (sets.Count == 0) summary = "Нет рабочих подходов";
            else
            {
                var reps = sets.Min(s => s.Reps) == sets.Max(s => s.Reps) ? sets[0].Reps.ToString() : $"{sets.Min(s => s.Reps)}–{sets.Max(s => s.Reps)}";
                var weight = sets.Min(s => s.Weight) == sets.Max(s => s.Weight) ? Number(sets[0].Weight) : $"{Number(sets.Min(s => s.Weight))}–{Number(sets.Max(s => s.Weight))}";
                summary = $"{sets.Count} × {reps} · {weight} кг";
            }
            return new WorkoutExercisePreview(e.Id, name, summary, PhotoFor(name));
        }).ToList();
        return new(workout.Id, workout.Date, workout.Title ?? "Тренировка", workout.WorkoutExercises.Count,
            DashboardData.WorkingSets(workout).Count(), DashboardData.Volume(workout), exercises.FirstOrDefault()?.Photo ?? "bench",
            workout.TrainingProgramId, programs.FirstOrDefault(p => p.Id == workout.TrainingProgramId)?.Name ?? (workout.TrainingProgramId.HasValue ? "Программа" : "Без программы"), exercises);
    }

    private static string PhotoFor(string name) => name.Contains("планк", StringComparison.OrdinalIgnoreCase) ? "plank" :
        name.Contains("ногами", StringComparison.OrdinalIgnoreCase) || name.Contains("Leg Press", StringComparison.OrdinalIgnoreCase) ? "leg-press" :
        name.Contains("присед", StringComparison.OrdinalIgnoreCase) || name.Contains("Squat", StringComparison.OrdinalIgnoreCase) ? "squat" :
        name.Contains("тяг", StringComparison.OrdinalIgnoreCase) || name.Contains("Deadlift", StringComparison.OrdinalIgnoreCase) ? "deadlift" : "bench";

    private static List<WorkoutHistoryItem> ReferenceHistory() => [
        new(-1, new(2026, 9, 30), "Ноги и кор", 4, 12, 3120, "history-1", -1, "Сила и форма", [
            new(0, "Приседания со штангой", "3 × 8 · 100 кг", "squat"),
            new(0, "Жим ногами в тренажёре", "3 × 10 · 180 кг", "leg-press"),
            new(0, "Румынская тяга", "3 × 8 · 90 кг", "deadlift"),
            new(0, "Планка", "3 × 45 сек · —", "plank")]),
        new(-2, new(2026, 9, 28), "Верх тела — сила", 4, 12, 2880, "history-2", -1, "Сила и форма", [
            new(0, "Жим лёжа", "3 × 8 · 60 кг", "bench"), new(0, "Тяга штанги в наклоне", "3 × 10 · 60 кг", "deadlift"),
            new(0, "Жим над головой", "3 × 8 · 40 кг", "bench"), new(0, "Планка", "3 × 45 сек · —", "plank")]),
        new(-3, new(2026, 9, 25), "Ноги и кор", 4, 12, 3060, "history-3", -1, "Сила и форма", [
            new(0, "Приседания со штангой", "3 × 8 · 97,5 кг", "squat"), new(0, "Жим ногами в тренажёре", "3 × 10 · 180 кг", "leg-press"),
            new(0, "Румынская тяга", "3 × 8 · 90 кг", "deadlift"), new(0, "Планка", "3 × 45 сек · —", "plank")]),
        new(-4, new(2026, 9, 23), "Верх тела — сила", 4, 12, 2820, "history-4", -1, "Сила и форма", [
            new(0, "Жим лёжа", "3 × 8 · 57,5 кг", "bench"), new(0, "Тяга штанги в наклоне", "3 × 10 · 60 кг", "deadlift"),
            new(0, "Жим над головой", "3 × 8 · 40 кг", "bench"), new(0, "Планка", "3 × 45 сек · —", "plank")])
    ];
}
