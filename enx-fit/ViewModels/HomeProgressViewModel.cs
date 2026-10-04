using enx_fit.Extensions;
using enx_fit.Models;
using enx_fit.Pages;

namespace enx_fit.ViewModels;

public sealed record HomeProgressResult(DateOnly Date, int WorkoutId, decimal Weight, int Reps);

public sealed class HomeProgressViewModel
{
    public IReadOnlyList<ProgressExerciseOption> Exercises { get; }
    public int? ExerciseId { get; }
    public string ExerciseName { get; }
    public IReadOnlyList<HomeProgressResult> Results { get; }
    public HomeProgressResult? Last => Results.LastOrDefault();
    public decimal? Change => Results.Count > 1 ? Results[^1].Weight - Results[0].Weight : null;

    public HomeProgressViewModel(DashboardModel model)
    {
        if (model.Reference)
        {
            Exercises = [new(1, "Жим лёжа"), new(2, "Приседания")];
            ExerciseId = model.ProgressExercise == 2 ? 2 : 1;
            ExerciseName = Exercises.Single(e => e.Id == ExerciseId).Name;
            Results = Enumerable.Range(0, 6).Select(i => new HomeProgressResult(new DateOnly(2026, 9, 1).AddDays(i * 6), -1,
                (ExerciseId == 2 ? 70m : 55m) + i / 2 * 2.5m, 10)).ToList();
            return;
        }
        var observations = model.Data.Completed.Where(w => w.Date <= model.Data.Until)
            .OrderBy(w => w.Date).ThenBy(w => w.Id)
            .SelectMany(w => w.WorkoutExercises.Where(e => e.SetEntries.Any(ValidSet)).Select(e => new
            {
                w.Date, WorkoutId = w.Id, e.ExerciseId, Name = ExercisePresentation.DisplayName(e.Exercise),
                Best = e.SetEntries.Where(ValidSet).OrderByDescending(s => s.Weight).ThenByDescending(s => s.Reps).First()
            })).ToList();
        Exercises = observations.GroupBy(o => o.ExerciseId).OrderByDescending(g => g.Last().Date)
            .ThenByDescending(g => g.Last().WorkoutId).Select(g => new ProgressExerciseOption(g.Key, g.Last().Name)).ToList();
        ExerciseId = Exercises.Any(e => e.Id == model.ProgressExercise) ? model.ProgressExercise : Exercises.FirstOrDefault()?.Id;
        ExerciseName = Exercises.FirstOrDefault(e => e.Id == ExerciseId)?.Name ?? "Результаты упражнений";
        Results = observations.Where(o => o.ExerciseId == ExerciseId && o.Date >= model.Data.Until.AddDays(1 - model.ChartWeeks * 7))
            .GroupBy(o => o.WorkoutId).Select(g => g.OrderByDescending(o => o.Best.Weight).ThenByDescending(o => o.Best.Reps).First())
            .Select(o => new HomeProgressResult(o.Date, o.WorkoutId, o.Best.Weight, o.Best.Reps)).TakeLast(12).ToList();
    }

    private static bool ValidSet(SetEntry set) => set.IsCompleted && !set.IsSkipped && !set.IsWarmup && set.Weight > 0 && set.Reps > 0;
}
