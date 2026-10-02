using enx_fit.Models;
using enx_fit.Pages.Programs;
using enx_fit.Services;

namespace enx_fit.ViewModels;

public sealed record ProgramOverviewCard(TrainingProgram Program, IReadOnlyList<WorkoutSession> Sessions, string Photo)
{
    public int Total => ProgramSchedule.Occurrences(Program).Count();
    public int Done => ProgramSchedule.Done(Program, Sessions);
    public int Percent => Total > 0 ? Math.Min(100, Done * 100 / Total) : 0;
    public ProgramOccurrence? Next => ProgramSchedule.Next(Program, Sessions);
    public bool Finished => Total > 0 && Done >= Total;
    public string Status => Program.IsTemplate ? ProgramSchedule.LevelName(Program.Level) : Program.IsArchived ? "В архиве" : Finished ? "Завершена" : Program.StartDate.HasValue ? "Активная программа" : "Готова к старту";
}

public sealed record ProgramOverviewDay(DateOnly Date, bool Training, bool Completed, bool Today, string Title);
public sealed record ProgramOverviewMenu(IndexModel Page, ProgramOverviewCard Card);

public sealed class ProgramsOverviewViewModel
{
    public ProgramOverviewCard? Active { get; }
    public IReadOnlyList<ProgramOverviewCard> Others { get; }
    public IReadOnlyList<ProgramOverviewDay> Week { get; }
    public DateOnly Today { get; }
    public int CurrentWeek { get; }
    public ProgramOccurrence? Next { get; }
    public string NextTitle => Next?.Workout.Name ?? (Active is null ? "Выберите программу" : Active.Total == 0 ? "Добавьте расписание" : "Нет ближайших тренировок");

    public ProgramsOverviewViewModel(IndexModel model)
    {
        Today = model.Reference ? new DateOnly(2026, 10, 2) : DateOnly.FromDateTime(DateTime.UtcNow);
        var cards = model.Reference && model.Tab == "mine" && !model.Archived ? ReferenceCards() :
            model.Items.Select((p, i) => new ProgramOverviewCard(p, model.Sessions.GetValueOrDefault(p.Id) ?? [], i % 2 == 0 ? "bench" : "dumbbells")).ToList();
        if (model.Tab == "mine" && !model.Archived)
            Active = cards.FirstOrDefault(c => c.Program.StartDate.HasValue && !c.Finished);
        var others = cards.Where(c => c != Active).ToList();
        Others = model.All || model.Tab != "mine" || model.Archived ? others : others.Take(2).ToList();
        CurrentWeek = Active?.Program.StartDate is { } start ? Math.Clamp((Today.DayNumber - start.DayNumber) / 7 + 1, 1, Active.Program.Weeks) : 0;
        Next = model.Reference && Active is { } reference ? ProgramSchedule.Occurrences(reference.Program).FirstOrDefault(o => o.Date >= Today) : Active?.Next;
        var monday = Today.AddDays(-((int)Today.DayOfWeek + 6) % 7 + model.WeekOffset * 7);
        var occurrences = Active is { } active ? ProgramSchedule.Occurrences(active.Program).ToList() : [];
        Week = Enumerable.Range(0, 7).Select(i =>
        {
            var date = monday.AddDays(i);
            var scheduled = occurrences.Where(o => o.Date == date).ToList();
            var done = scheduled.Count > 0 ? scheduled.All(o => Active!.Sessions.Any(s => s.ProgramWorkoutKey == o.Workout.Key && s.ScheduledDate == date && s.CompletedAtUtc.HasValue)) : date < Today;
            return new ProgramOverviewDay(date, scheduled.Count > 0, done, date == Today, scheduled.FirstOrDefault()?.Workout.Name ?? "Отдых");
        }).ToList();
    }

    private static List<ProgramOverviewCard> ReferenceCards()
    {
        var program = new TrainingProgram { Id = -10000, Name = "Сила и баланс", Goal = "Набор силы", Weeks = 6, DaysPerWeek = 3, StartDate = new(2026, 9, 14),
            Workouts = [new() { Name = "Верх тела — сила", DayOfWeek = 1, Order = 0 }, new() { Name = "Верх тела — сила", DayOfWeek = 3, Order = 1 }, new() { Name = "Ноги и кор", DayOfWeek = 5, Order = 2 }] };
        foreach (var workout in program.Workouts) workout.Exercises = Enumerable.Range(1, 4).Select(id => new ProgramWorkoutExercise { ExerciseId = id }).ToList();
        var sessions = ProgramSchedule.Occurrences(program).Where(o => o.Date < new DateOnly(2026, 10, 2)).Select(o => new WorkoutSession { ProgramWorkoutKey = o.Workout.Key,
            ScheduledDate = o.Date, Date = o.Date, CompletedAtUtc = o.Date.ToDateTime(new TimeOnly(12, 0)) }).ToList();
        return [new(program, sessions, "hero"),
            new(new() { Id = -10001, Name = "Фулбоди · старт", Goal = "Общая подготовка", Description = "Универсальная программа для развития силы и общей подготовки.", Weeks = 4, DaysPerWeek = 3 }, [], "bench"),
            new(new() { Id = -10002, Name = "Верх / низ", Goal = "Набор мышечной массы", Description = "Классический сплит для системного роста силы и мышечной массы.", Weeks = 8, DaysPerWeek = 4 }, [], "dumbbells")];
    }
}
