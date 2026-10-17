using enx_fit.Models;

namespace enx_fit.Services.Programs;

public sealed record ProgramOccurrence(ProgramWorkout Workout, DateOnly Date, DateOnly? OriginalDate = null)
{
    public DateOnly SlotDate => OriginalDate ?? Date;
    public bool IsMoved => Date != SlotDate;
}
public static class ProgramSchedule
{
    public static string LevelName(ProgramLevel level) => level switch
    { ProgramLevel.Beginner => "Начинающий", ProgramLevel.Intermediate => "Средний", ProgramLevel.Advanced => "Продвинутый", _ => "" };
    public static readonly string[] DayNames = ["ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "ВС"];
    public static IEnumerable<ProgramOccurrence> Occurrences(TrainingProgram p)
    {
        var changes = p.ScheduleChanges.ToDictionary(c => (c.ProgramWorkoutKey, c.OriginalDate), c => c.Date);
        return BaseOccurrences(p).Select(o => changes.TryGetValue((o.Workout.Key, o.Date), out var date)
            ? new ProgramOccurrence(o.Workout, date, o.Date) : o).OrderBy(o => o.Date).ThenBy(o => o.Workout.Order);
    }
    public static IEnumerable<ProgramOccurrence> BaseOccurrences(TrainingProgram p)
    {
        if (p.StartDate is not { } start) yield break;
        // A cycle is Weeks × 7 days from the selected start, including a partial calendar week.
        for (var offset = 0; offset < p.Weeks * 7; offset++)
        {
            var date = start.AddDays(offset);
            var day = ((int)date.DayOfWeek + 6) % 7 + 1;
            foreach (var workout in p.Workouts.Where(w => w.DayOfWeek == day)) yield return new(workout, date);
        }
    }
    public static ProgramOccurrence? Next(TrainingProgram p, IReadOnlyList<WorkoutSession> sessions, DateOnly? today = null) =>
        Occurrences(p).FirstOrDefault(o => sessions.Any(s => Matches(s, o) && s.IsActive)) ??
        Occurrences(p).FirstOrDefault(o => o.Date >= (today ?? DateOnly.FromDateTime(DateTime.UtcNow)) && !sessions.Any(s => Matches(s, o) && s.State == WorkoutStatus.Completed));
    public static int Done(TrainingProgram p, IReadOnlyList<WorkoutSession> sessions) => Occurrences(p).Count(o =>
        sessions.Any(s => Matches(s, o) && s.State == WorkoutStatus.Completed));
    public static bool Matches(WorkoutSession session, ProgramOccurrence occurrence) =>
        session.ProgramWorkoutKey == occurrence.Workout.Key && session.ScheduledDate == occurrence.SlotDate;
    public static int CurrentWeek(TrainingProgram p) => p.StartDate is { } start
        ? Math.Clamp((DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - start.DayNumber) / 7 + 1, 1, p.Weeks) : 0;
}
